using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Interaction.Events;

public sealed record InteractionUpdated(
    InteractionId InteractionId,
    string? UpdatedSubject,
    string? UpdatedDescription,
    DateTime? UpdatedScheduledAt,
    DateTime? UpdatedCompletedAt,
    TimeSpan? UpdatedDuration) : DomainEventBase(InteractionId.ToString(), nameof(Interaction))
{
}
