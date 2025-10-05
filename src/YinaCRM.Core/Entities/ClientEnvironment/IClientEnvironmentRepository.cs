using YinaCRM.Core.Abstractions;

namespace YinaCRM.Core.Entities.ClientEnvironment;

/// <summary>
/// Repository interface for ClientEnvironment aggregate root.
/// Provides persistence operations for ClientEnvironment.
/// </summary>
public interface IClientEnvironmentRepository : IRepository<ClientEnvironment, ClientEnvironmentId>
{
    // No additional methods - queries for environments by ClientId 
    // should be handled by query services following CQRS
}
