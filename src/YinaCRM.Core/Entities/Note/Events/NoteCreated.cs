using YinaCRM.Core.Entities.Note.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Note.Events;

public sealed record NoteCreated(
    NoteId NoteId,
    VisibilityCode Visibility,
    ActorKindCode CreatedByKind,
    Guid CreatedById,
    DateTime CreatedAtUtc) : DomainEventBase(NoteId.ToString(), nameof(Note))
{
}


