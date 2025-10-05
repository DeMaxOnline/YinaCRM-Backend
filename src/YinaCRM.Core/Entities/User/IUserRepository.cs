using YinaCRM.Core.Abstractions;
using YinaCRM.Core.ValueObjects.Identity.AuthSubjectVO;

namespace YinaCRM.Core.Entities.User;

/// <summary>
/// Repository interface for User aggregate root.
/// Provides domain-specific operations for User persistence.
/// </summary>
public interface IUserRepository : IRepository<User, UserId>
{
    /// <summary>
    /// Retrieves a user by their authentication subject identifier.
    /// Required for authentication and authorization flows.
    /// </summary>
    Task<User?> GetByAuthSubjectAsync(AuthSubject authSub, CancellationToken cancellationToken = default);
}
