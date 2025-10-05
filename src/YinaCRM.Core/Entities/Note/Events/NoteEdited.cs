using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Note.Events;

public sealed record NoteEdited(
    NoteId NoteId) : DomainEventBase(NoteId.ToString(), nameof(Note))
{
}


