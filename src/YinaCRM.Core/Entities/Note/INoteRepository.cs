using YinaCRM.Core.Abstractions;

namespace YinaCRM.Core.Entities.Note;

/// <summary>
/// Repository interface for Note aggregate root.
/// Provides persistence operations for Note.
/// </summary>
public interface INoteRepository : IRepository<Note, NoteId>
{
    // No additional methods - queries for notes linked to specific entities
    // should be handled by query services following CQRS
}
