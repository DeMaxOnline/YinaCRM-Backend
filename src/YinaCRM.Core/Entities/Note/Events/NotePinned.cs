using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.Note.Events;

public sealed record NotePinned(
    NoteId NoteId) : DomainEventBase(NoteId.ToString(), nameof(Note))
{
}


