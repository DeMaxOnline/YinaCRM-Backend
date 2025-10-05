using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Interaction.Events;

public sealed record InteractionParticipantAdded(
    InteractionId InteractionId,
    ActorKindCode ParticipantKind,
    Guid ParticipantId,
    ParticipantRoleCode Role) : DomainEventBase(InteractionId.ToString(), nameof(Interaction))
{
}
