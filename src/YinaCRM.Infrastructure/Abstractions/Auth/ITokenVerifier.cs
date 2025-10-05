using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Auth;

/// <summary>
/// Verifies bearer tokens (id/access) issued by an upstream identity provider.
/// </summary>
public interface ITokenVerifier
{
    Task<Result<AuthenticatedPrincipal>> VerifyAsync(string token, CancellationToken cancellationToken = default);
}
