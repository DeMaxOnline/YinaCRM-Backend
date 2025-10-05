using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Auth.Auth0;

public sealed class Auth0IdentityProvider : IIdentityProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<Auth0Options> _optionsMonitor;
    private readonly ILogger<Auth0IdentityProvider> _logger;
    private readonly TimeProvider _timeProvider;

    public Auth0IdentityProvider(
        HttpClient httpClient,
        IOptionsMonitor<Auth0Options> optionsMonitor,
        ILogger<Auth0IdentityProvider> logger,
        TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<TokenExchangeResult>> ExchangeCodeAsync(
        CodeExchangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = _optionsMonitor.CurrentValue;
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["code"] = request.Code,
            ["redirect_uri"] = request.RedirectUri,
            ["audience"] = options.Audience,
        };

        if (!string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            payload["code_verifier"] = request.CodeVerifier;
        }

        if (!string.IsNullOrWhiteSpace(request.TenantHint))
        {
            payload["organization"] = request.TenantHint;
        }

        return await ExecuteTokenRequestAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<TokenRefreshResult>> RefreshAsync(
        TokenRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = _optionsMonitor.CurrentValue;
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["refresh_token"] = request.RefreshToken,
        };

        if (!string.IsNullOrWhiteSpace(request.TenantHint))
        {
            payload["organization"] = request.TenantHint;
        }

        var result = await ExecuteTokenRequestAsync(payload, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure<TokenRefreshResult>(result.Error);
        }

        var exchange = result.Value;
        return Result.Success(new TokenRefreshResult(
            exchange.AccessToken,
            exchange.IdToken,
            exchange.RefreshToken ?? request.RefreshToken,
            exchange.ExpiresAt,
            exchange.CustomParameters));
    }

    public async Task<Result> RevokeAsync(
        TokenRevokeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = _optionsMonitor.CurrentValue;
        var payload = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["token"] = request.Token,
        };

        payload["token_type_hint"] = request.Type switch
        {
            TokenRevokeType.AccessToken => "access_token",
            TokenRevokeType.RefreshToken => "refresh_token",
            _ => "refresh_token",
        };

        var response = await _httpClient.PostAsync("/oauth/revoke", new FormUrlEncodedContent(payload), cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning(
            "Auth0 revoke failed with status {StatusCode}: {Body}",
            (int)response.StatusCode,
            body);
        return Result.Failure(InfrastructureErrors.ExternalDependency(
            "AUTH0_REVOKE_FAILED",
            "Failed to revoke token.",
            response.StatusCode));
    }

    private async Task<Result<TokenExchangeResult>> ExecuteTokenRequestAsync(
        Dictionary<string, string> formParameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsync("/oauth/token", new FormUrlEncodedContent(formParameters), cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var tokenResponse = await JsonSerializer.DeserializeAsync<Auth0TokenResponse>(content, SerializerOptions, cancellationToken).ConfigureAwait(false);
            tokenResponse ??= new Auth0TokenResponse();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Auth0 token request failed with status {StatusCode}: {Error} {Description}",
                    (int)response.StatusCode,
                    tokenResponse.Error,
                    tokenResponse.ErrorDescription);

                return Result.Failure<TokenExchangeResult>(InfrastructureErrors.ExternalDependency(
                    "AUTH0_TOKEN_FAILED",
                    tokenResponse.ErrorDescription ?? "Auth0 token endpoint returned an error.",
                    response.StatusCode));
            }

            if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                return Result.Failure<TokenExchangeResult>(InfrastructureErrors.AuthenticationFailure("Access token missing in response."));
            }

            var now = _timeProvider.GetUtcNow();
            var expiresAt = tokenResponse.ExpiresIn > 0
                ? now.AddSeconds(tokenResponse.ExpiresIn)
                : now.AddMinutes(60);

            var custom = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(tokenResponse.Scope))
            {
                custom["scope"] = tokenResponse.Scope;
            }

            if (!string.IsNullOrWhiteSpace(tokenResponse.TokenType))
            {
                custom["token_type"] = tokenResponse.TokenType;
            }

            var exchangeResult = new TokenExchangeResult(
                tokenResponse.AccessToken!,
                tokenResponse.IdToken,
                tokenResponse.RefreshToken,
                expiresAt,
                custom);

            return Result.Success(exchangeResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth0 token request failed.");
            return Result.Failure<TokenExchangeResult>(InfrastructureErrors.Unexpected("Token request failed."));
        }
    }

    private sealed record Auth0TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }

        [JsonPropertyName("id_token")] public string? IdToken { get; init; }

        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }

        [JsonPropertyName("scope")] public string? Scope { get; init; }

        [JsonPropertyName("token_type")] public string? TokenType { get; init; }

        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }

        [JsonPropertyName("error")] public string? Error { get; init; }

        [JsonPropertyName("error_description")] public string? ErrorDescription { get; init; }
    }
}

