using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Auth;

/// <summary>
/// Represents an upstream identity provider capable of exchanging authorization codes, refreshing tokens, and revoking sessions.
/// </summary>
public interface IIdentityProvider
{
    Task<Result<TokenExchangeResult>> ExchangeCodeAsync(CodeExchangeRequest request, CancellationToken cancellationToken = default);

    Task<Result<TokenRefreshResult>> RefreshAsync(TokenRefreshRequest request, CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(TokenRevokeRequest request, CancellationToken cancellationToken = default);
}
