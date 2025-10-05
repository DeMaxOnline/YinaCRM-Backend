using YinaCRM.Core.ValueObjects.Codes.RelatedTypeCodeVO;

namespace YinaCRM.Core.Entities.Note;

/// <summary>
/// Link between a note and a related entity. Composite key: NoteId + RelatedType + RelatedId.
/// </summary>
public sealed class NoteLink
{
    public NoteLink(NoteId noteId, RelatedTypeCode relatedType, Guid relatedId)
    {
        NoteId = noteId;
        RelatedType = relatedType;
        RelatedId = relatedId;
    }

    public NoteId NoteId { get; }
    public RelatedTypeCode RelatedType { get; }
    public Guid RelatedId { get; }
}

