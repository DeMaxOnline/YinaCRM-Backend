using System.ComponentModel.DataAnnotations;

namespace YinaCRM.Infrastructure.Auth.Auth0;

public sealed class Auth0Options
{
    [Required]
    public string Domain { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public string[] AllowedAlgorithms { get; init; } = ["RS256"];

    public string TenantIdClaim { get; init; } = "org_id";

    public string[] TenantFallbackClaims { get; init; } = ["https://yina.crm/tenant", "tenant_id"];

    public bool RequireTenantId { get; init; } = true;

    public string EmailClaim { get; init; } = "email";

    public string NameClaim { get; init; } = "name";

    public string[] RoleClaims { get; init; } = ["https://yina.crm/roles", "roles"];

    public string ScopeClaim { get; init; } = "scope";

    public Dictionary<string, string> CustomClaimMappings { get; init; } = new();

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan JwksRefreshInterval { get; init; } = TimeSpan.FromHours(6);

    public bool RequireHttpsMetadata { get; init; } = true;

    public ManagementApiOptions Management { get; init; } = new();

    public WebhookOptions Webhooks { get; init; } = new();

    public List<string> AdditionalAudiences { get; init; } = new();

    public IReadOnlyCollection<string> EffectiveAudiences
    {
        get
        {
            var audiences = new List<string> { Audience };
            if (AdditionalAudiences is { Count: > 0 })
            {
                audiences.AddRange(AdditionalAudiences);
            }

            return audiences;
        }
    }

    public sealed class ManagementApiOptions
    {
        public string Audience { get; init; } = string.Empty;

        public string ClientId { get; init; } = string.Empty;

        public string ClientSecret { get; init; } = string.Empty;

        public TimeSpan TokenCacheDuration { get; init; } = TimeSpan.FromMinutes(50);
    }

    public sealed class WebhookOptions
    {
        public string SignatureHeader { get; init; } = "x-auth0-signature";

        public string Algorithm { get; init; } = "sha256";
    }
}
