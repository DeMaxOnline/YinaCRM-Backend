using System.Collections.Immutable;
using System.Security.Claims;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Auth.Auth0;

internal sealed class Auth0PrincipalFactory
{
    private readonly Auth0Options _options;

    public Auth0PrincipalFactory(Auth0Options options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Result<AuthenticatedPrincipal> Create(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.AuthenticationFailure("Subject claim missing"));
        }

        var tenantId = ResolveTenant(principal);
        if (_options.RequireTenantId && string.IsNullOrWhiteSpace(tenantId))
        {
            return Result.Failure<AuthenticatedPrincipal>(InfrastructureErrors.AuthenticationFailure("Tenant claim missing"));
        }

        var email = ResolveClaim(principal, _options.EmailClaim) ?? string.Empty;
        var displayName = ResolveClaim(principal, _options.NameClaim) ?? email ?? subject;
        var roles = ResolveMultiValue(principal, _options.RoleClaims);
        var scopes = ResolveScopes(principal, _options.ScopeClaim);
        var customClaims = ResolveCustomClaims(principal, _options.CustomClaimMappings);

        var authenticated = new AuthenticatedPrincipal(
            subject,
            tenantId,
            email!,
            displayName,
            roles.ToImmutableArray(),
            scopes.ToImmutableArray(),
            customClaims);

        return Result.Success(authenticated);
    }

    private string? ResolveTenant(ClaimsPrincipal principal)
    {
        var tenant = ResolveClaim(principal, _options.TenantIdClaim);
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            return tenant;
        }

        foreach (var claim in _options.TenantFallbackClaims)
        {
            tenant = ResolveClaim(principal, claim);
            if (!string.IsNullOrWhiteSpace(tenant))
            {
                return tenant;
            }
        }

        return null;
    }

    private static string? ResolveClaim(ClaimsPrincipal principal, string claimTypeOrName)
    {
        if (string.IsNullOrWhiteSpace(claimTypeOrName))
        {
            return null;
        }

        var claim = principal.FindFirst(claimTypeOrName);
        if (claim is null && claimTypeOrName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            claim = principal.Claims.FirstOrDefault(c => string.Equals(c.Type, claimTypeOrName, StringComparison.OrdinalIgnoreCase));
        }

        return claim?.Value;
    }

    private static IEnumerable<string> ResolveMultiValue(ClaimsPrincipal principal, IReadOnlyCollection<string> claimTypes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claimType in claimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                if (!string.IsNullOrWhiteSpace(claim.Value))
                {
                    set.Add(claim.Value);
                }
            }

            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value) && value.Contains(' '))
            {
                foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    set.Add(token);
                }
            }
        }

        return set;
    }

    private static IEnumerable<string> ResolveScopes(ClaimsPrincipal principal, string claimType)
    {
        var scopeClaim = ResolveClaim(principal, claimType);
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            yield break;
        }

        foreach (var scope in scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return scope;
        }
    }

    private static ImmutableDictionary<string, string> ResolveCustomClaims(
        ClaimsPrincipal principal,
        IReadOnlyDictionary<string, string> mappings)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in mappings)
        {
            var value = ResolveClaim(principal, mapping.Key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder[mapping.Value] = value;
            }
        }

        return builder.ToImmutable();
    }
}



