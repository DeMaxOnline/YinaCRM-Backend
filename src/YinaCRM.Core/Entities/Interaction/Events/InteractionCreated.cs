using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Interaction.Events;

public sealed record InteractionCreated(
    InteractionId InteractionId,
    InteractionTypeCode Type,
    InteractionDirectionCode Direction,
    string Subject,
    string? Description,
    DateTime? ScheduledAt,
    DateTime? CompletedAt,
    TimeSpan? Duration,
    ActorKindCode CreatedByKind,
    Guid CreatedById,
    DateTime CreatedAt,
    List<InteractionCreated.ParticipantInfo> Participants,
    List<InteractionCreated.LinkInfo> Links) : DomainEventBase(InteractionId.ToString(), nameof(Interaction))
{
    public record ParticipantInfo(
        ActorKindCode ParticipantKind,
        Guid ParticipantId,
        ParticipantRoleCode Role);

    public record LinkInfo(
        string RelatedType,
        Guid RelatedId);
}
