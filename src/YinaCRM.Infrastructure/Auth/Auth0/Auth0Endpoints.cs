namespace YinaCRM.Infrastructure.Auth.Auth0;

internal static class Auth0Endpoints
{
    public static Uri BuildAuthority(string domain)
    {
        domain = domain.Trim();
        if (!domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            domain = $"https://{domain}";
        }

        return new Uri(domain.TrimEnd('/'));
    }

    public static Uri BuildTokenEndpoint(string domain) => new(BuildAuthority(domain), "/oauth/token");

    public static Uri BuildAuthorizeEndpoint(string domain) => new(BuildAuthority(domain), "/authorize");

    public static Uri BuildJwksEndpoint(string domain) => new(BuildAuthority(domain), "/.well-known/jwks.json");

    public static Uri BuildOidcConfigurationEndpoint(string domain) => new(BuildAuthority(domain), "/.well-known/openid-configuration");

    public static Uri BuildManagementUsersEndpoint(string domain, string userId)
    {
        return new Uri(BuildAuthority(domain), $"/api/v2/users/{Uri.EscapeDataString(userId)}");
    }

    public static Uri BuildWebhookSecretsEndpoint(string domain) => new(BuildAuthority(domain), "/api/v2/hooks/secrets");
}
