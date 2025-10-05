namespace YinaCRM.Infrastructure.Abstractions.Auth;

public sealed record CodeExchangeRequest(
    string Code,
    string RedirectUri,
    string? CodeVerifier,
    string TenantHint);

public sealed record TokenExchangeResult(
    string AccessToken,
    string? IdToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> CustomParameters);

public sealed record TokenRefreshRequest(
    string RefreshToken,
    string TenantHint);

public sealed record TokenRefreshResult(
    string AccessToken,
    string? IdToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> CustomParameters);

public sealed record TokenRevokeRequest(
    string Token,
    TokenRevokeType Type,
    string TenantHint);

public enum TokenRevokeType
{
    AccessToken,
    RefreshToken,
    All,
}
