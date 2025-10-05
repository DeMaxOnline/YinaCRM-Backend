using YinaCRM.Core.Abstractions;

namespace YinaCRM.Core.Entities.ClientUser;

/// <summary>
/// Repository interface for ClientUser aggregate root.
/// Provides persistence operations for ClientUser.
/// </summary>
public interface IClientUserRepository : IRepository<ClientUser, ClientUserId>
{
    // No additional methods - complex queries should be handled 
    // by query services following CQRS
}
