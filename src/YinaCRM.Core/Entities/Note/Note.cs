using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.Note.Events;
using YinaCRM.Core.Entities.Note.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Note;

/// <summary>
/// Note aggregate with body, visibility, pin state and tags. Emits simple lifecycle events.
/// </summary>
public sealed partial class Note : AggregateRoot<NoteId>
{
    private readonly List<Tag> _tags = new();

        private Note()
    {
        EnsurePersistenceTagsInitialized();
    }

    private Note(
        NoteId id,
        Body body,
        VisibilityCode visibility,
        bool pinned,
        IEnumerable<Tag>? tags,
        ActorKindCode createdByKind,
        Guid createdById,
        DateTime createdAtUtc)
    {
        Id = id;
        Body = body;
        Visibility = visibility;
        Pinned = pinned;
        if (tags is not null) _tags.AddRange(tags);

        EnsurePersistenceTagsInitialized();
        SyncPersistenceTagsFromDomain();

        CreatedByKind = createdByKind;
        CreatedById = createdById;
        CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        EditedAt = null;
    }

    public override NoteId Id { get; protected set; }
    public Body Body { get; private set; }
    public VisibilityCode Visibility { get; private set; }
    public bool Pinned { get; private set; }
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    public ActorKindCode CreatedByKind { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime? EditedAt { get; private set; }


    public static Result<Note> Create(
        Body body,
        VisibilityCode visibility,
        bool pinned,
        IEnumerable<Tag>? tags,
        ActorKindCode createdByKind,
        Guid createdById,
        DateTime? createdAtUtc = null)
        => Create(
            NoteId.New(), body, visibility, pinned, tags, createdByKind, createdById, createdAtUtc);

    public static Result<Note> Create(
        NoteId id,
        Body body,
        VisibilityCode visibility,
        bool pinned,
        IEnumerable<Tag>? tags,
        ActorKindCode createdByKind,
        Guid createdById,
        DateTime? createdAtUtc = null)
    {
        if (IsClientUser(createdByKind) && visibility.IsInternal)
            return Result<Note>.Failure(Errors.InternalNotAllowedForClientUsers());

        var note = new Note(id, body, visibility, pinned, tags, createdByKind, createdById, createdAtUtc ?? DateTime.UtcNow);
        note.RaiseEvent(new NoteCreated(note.Id, note.Visibility, note.CreatedByKind, note.CreatedById, note.CreatedAt));
        return Result<Note>.Success(note);
    }

    public Result Edit(Body newBody)
    {
        Body = newBody;
        RaiseEvent(new NoteEdited(Id));
        return Result.Success();
    }

    public Result Pin()
    {
        if (Pinned) return Result.Success();
        Pinned = true;
        RaiseEvent(new NotePinned(Id));
        return Result.Success();
    }

    public Result Unpin()
    {
        if (!Pinned) return Result.Success();
        Pinned = false;
        RaiseEvent(new NoteUnpinned(Id));
        return Result.Success();
    }

    /// <summary>
    /// Applies events to rebuild the aggregate state during event sourcing replay.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    protected override void ApplyEvent(IDomainEvent @event)
    {
        switch (@event)
        {
            case NoteCreated created:
                Id = created.NoteId;
                Visibility = created.Visibility;
                CreatedByKind = created.CreatedByKind;
                CreatedById = created.CreatedById;
                CreatedAt = created.CreatedAtUtc;
                break;
                
            case NoteEdited edited:
                EditedAt = edited.OccurredAtUtc;
                UpdatedAt = edited.OccurredAtUtc;
                break;
                
            case NotePinned pinned:
                Pinned = true;
                EditedAt = pinned.OccurredAtUtc;
                UpdatedAt = pinned.OccurredAtUtc;
                break;
                
            case NoteUnpinned unpinned:
                Pinned = false;
                EditedAt = unpinned.OccurredAtUtc;
                UpdatedAt = unpinned.OccurredAtUtc;
                break;
                
            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }

    private static bool IsClientUser(ActorKindCode code)
        => string.Equals(code.ToString(), "ClientUser", StringComparison.OrdinalIgnoreCase);

    private static class Errors
    {
        public static Error InternalNotAllowedForClientUsers() => Error.Create("NOTE_INTERNAL_FOR_CLIENTUSER", "Client users cannot create internal notes", 403);
    }
}


