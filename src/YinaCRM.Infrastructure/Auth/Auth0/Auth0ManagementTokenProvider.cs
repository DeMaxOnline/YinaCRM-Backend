using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Auth.Auth0;

internal sealed class Auth0ManagementTokenProvider : IAuth0ManagementTokenSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<Auth0Options> _optionsMonitor;
    private readonly ILogger<Auth0ManagementTokenProvider> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt;

    public Auth0ManagementTokenProvider(
        HttpClient httpClient,
        IOptionsMonitor<Auth0Options> optionsMonitor,
        ILogger<Auth0ManagementTokenProvider> logger,
        TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<string>> GetTokenAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        if (_cachedToken is not null && _expiresAt > now.AddMinutes(1))
        {
            return Result.Success(_cachedToken);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            now = _timeProvider.GetUtcNow();
            if (_cachedToken is not null && _expiresAt > now.AddMinutes(1))
            {
                return Result.Success(_cachedToken);
            }

            var options = _optionsMonitor.CurrentValue;
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = options.Management.ClientId,
                ["client_secret"] = options.Management.ClientSecret,
                ["audience"] = options.Management.Audience,
            };

            var response = await _httpClient.PostAsync("/oauth/token", new FormUrlEncodedContent(form), cancellationToken).ConfigureAwait(false);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<TokenResponse>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            payload ??= new TokenResponse();

            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                _logger.LogError(
                    "Auth0 management token fetch failed with status {StatusCode}: {Error} {Description}",
                    (int)response.StatusCode,
                    payload.Error,
                    payload.ErrorDescription);
                return Result.Failure<string>(InfrastructureErrors.ExternalDependency(
                    "AUTH0_MGMT_TOKEN_FAILED",
                    payload.ErrorDescription ?? "Failed to obtain Auth0 management token.",
                    response.StatusCode));
            }

            _cachedToken = payload.AccessToken;
            var lifetime = payload.ExpiresIn > 0 ? TimeSpan.FromSeconds(payload.ExpiresIn) : options.Management.TokenCacheDuration;
            _expiresAt = now.Add(lifetime);

            return Result.Success(_cachedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain Auth0 management token.");
            return Result.Failure<string>(InfrastructureErrors.Unexpected("Failed to obtain Auth0 management token."));
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }

        [JsonPropertyName("error")] public string? Error { get; init; }

        [JsonPropertyName("error_description")] public string? ErrorDescription { get; init; }
    }
}

