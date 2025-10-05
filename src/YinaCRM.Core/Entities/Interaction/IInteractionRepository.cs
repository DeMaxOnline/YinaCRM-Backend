using YinaCRM.Core.Abstractions;

namespace YinaCRM.Core.Entities.Interaction;

/// <summary>
/// Repository interface for Interaction aggregate root.
/// Provides persistence operations for Interaction.
/// </summary>
public interface IInteractionRepository : IRepository<Interaction, InteractionId>
{
    // No additional methods - complex queries should be handled 
    // by query services following CQRS
}
