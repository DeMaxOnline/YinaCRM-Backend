using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Interaction.Events;

public sealed record InteractionParticipantRemoved(
    InteractionId InteractionId,
    ActorKindCode ParticipantKind,
    Guid ParticipantId) : DomainEventBase(InteractionId.ToString(), nameof(Interaction))
{
}
