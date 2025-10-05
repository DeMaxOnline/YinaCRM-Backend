using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.Hardware.VOs;

namespace YinaCRM.Core.Entities.Hardware;

/// <summary>
/// Repository interface for Hardware aggregate root.
/// Provides domain-specific operations for Hardware persistence.
/// </summary>
public interface IHardwareRepository : IRepository<Hardware, HardwareId>
{
    /// <summary>
    /// Retrieves hardware by its external system identifier.
    /// Required for synchronization with external hardware management systems.
    /// </summary>
    Task<Hardware?> GetByExternalIdAsync(ExternalHardwareId externalId, CancellationToken cancellationToken = default);
}
