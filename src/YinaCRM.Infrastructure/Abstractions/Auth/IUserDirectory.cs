using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Auth;

/// <summary>
/// Synchronises and provisions users from the upstream identity provider into the CRM.
/// </summary>
public interface IUserDirectory
{
    Task<Result<UserDirectorySyncResult>> EnsureUserAsync(AuthenticatedPrincipal principal, CancellationToken cancellationToken = default);
}

public sealed record UserDirectorySyncResult(
    string InternalUserId,
    bool WasCreated,
    IReadOnlyDictionary<string, string> Metadata);
