using YinaCRM.Core.Abstractions;

namespace YinaCRM.Core.Entities.Client;

/// <summary>
/// Repository interface for Client aggregate root.
/// Provides domain-specific operations for Client persistence.
/// </summary>
public interface IClientRepository : IRepository<Client, ClientId>
{
    /// <summary>
    /// Retrieves a client by their YinaYina system identifier.
    /// Required for legacy system integration.
    /// </summary>
    Task<Client?> GetByYinaYinaIdAsync(int yinaYinaId, CancellationToken cancellationToken = default);
}
