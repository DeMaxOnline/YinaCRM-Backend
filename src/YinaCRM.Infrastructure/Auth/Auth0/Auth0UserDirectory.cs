using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Auth.Auth0;

public sealed class Auth0UserDirectory : IUserDirectory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IAuth0ManagementTokenSource _tokenSource;
    private readonly ILogger<Auth0UserDirectory> _logger;

    public Auth0UserDirectory(
        HttpClient httpClient,
        IAuth0ManagementTokenSource tokenSource,
        ILogger<Auth0UserDirectory> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<UserDirectorySyncResult>> EnsureUserAsync(
        AuthenticatedPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var tokenResult = await _tokenSource.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<UserDirectorySyncResult>(tokenResult.Error);
        }

        var token = tokenResult.Value;
        var userId = principal.SubjectId;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/users/{Uri.EscapeDataString(userId)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Auth0 user {UserId} not found.", userId);
            return Result.Failure<UserDirectorySyncResult>(InfrastructureErrors.AuthenticationFailure("User does not exist in Auth0."));
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Auth0 user fetch failed for {UserId} with status {StatusCode}: {Body}",
                userId,
                (int)response.StatusCode,
                body);
            return Result.Failure<UserDirectorySyncResult>(InfrastructureErrors.ExternalDependency(
                "AUTH0_USER_FETCH_FAILED",
                "Failed to fetch user from Auth0.",
                response.StatusCode));
        }

        var user = await response.Content.ReadFromJsonAsync<Auth0UserResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? new Auth0UserResponse();

        var metadata = user.AppMetadata ?? new JsonObject();
        var crmMetadata = metadata["crm"] as JsonObject ?? new JsonObject();
        var tenants = crmMetadata["tenants"] as JsonArray ?? new JsonArray();

        var tenantId = principal.TenantId;
        var addedTenant = false;
        if (!string.IsNullOrWhiteSpace(tenantId) && !tenants.Any(node => node?.GetValue<string>() == tenantId))
        {
            tenants.Add(tenantId);
            crmMetadata["tenants"] = tenants;
            metadata["crm"] = crmMetadata;
            addedTenant = true;
        }

        if (addedTenant)
        {
            var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v2/users/{Uri.EscapeDataString(userId)}")
            {
                Content = JsonContent.Create(new { app_metadata = metadata }, options: SerializerOptions),
            };
            patchRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var patchResponse = await _httpClient.SendAsync(patchRequest, cancellationToken).ConfigureAwait(false);
            if (!patchResponse.IsSuccessStatusCode)
            {
                var body = await patchResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    "Auth0 user metadata update failed for {UserId} with status {StatusCode}: {Body}",
                    userId,
                    (int)patchResponse.StatusCode,
                    body);
                return Result.Failure<UserDirectorySyncResult>(InfrastructureErrors.ExternalDependency(
                    "AUTH0_USER_PATCH_FAILED",
                    "Failed to update Auth0 app metadata.",
                    patchResponse.StatusCode));
            }
        }

        var metadataDictionary = new Dictionary<string, string>
        {
            ["auth0_user_id"] = user.UserId ?? userId,
            ["email_verified"] = user.EmailVerified?.ToString() ?? "false",
        };

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            metadataDictionary["tenant_id"] = tenantId;
        }

        var syncResult = new UserDirectorySyncResult(
            InternalUserId: user.UserId ?? userId,
            WasCreated: false,
            Metadata: metadataDictionary);

        return Result.Success(syncResult);
    }

    private sealed record Auth0UserResponse
    {
        public string? UserId { get; init; }

        public string? Email { get; init; }

        public bool? EmailVerified { get; init; }

        public JsonObject? AppMetadata { get; init; }
    }
}
