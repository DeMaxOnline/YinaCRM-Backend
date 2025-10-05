using System.Collections.Immutable;
using System.Security.Claims;

namespace YinaCRM.Infrastructure.Abstractions.Auth;

/// <summary>
/// Represents an authenticated principal propagated from the infrastructure layer into the application boundary.
/// </summary>
public sealed record AuthenticatedPrincipal(
    string SubjectId,
    string? TenantId,
    string Email,
    string DisplayName,
    ImmutableArray<string> Roles,
    ImmutableArray<string> Scopes,
    ImmutableDictionary<string, string> CustomClaims)
{
    public ClaimsIdentity ToClaimsIdentity(string authenticationType = "Bearer")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, SubjectId),
        };

        if (!string.IsNullOrWhiteSpace(Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, Email));
        }

        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            claims.Add(new Claim(ClaimTypes.Name, DisplayName));
        }

        if (!string.IsNullOrWhiteSpace(TenantId))
        {
            claims.Add(new Claim("tenant_id", TenantId));
        }

        foreach (var role in Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var scope in Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        foreach (var kvp in CustomClaims)
        {
            claims.Add(new Claim(kvp.Key, kvp.Value));
        }

        return new ClaimsIdentity(claims, authenticationType);
    }
}
