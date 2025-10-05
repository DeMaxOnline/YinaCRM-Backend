using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.Interaction.Events;
using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Interaction;

/// <summary>
/// Interaction aggregate root representing communications between coworkers and clients.
/// Tracks calls, emails, meetings, chats, and remote sessions with participants and related entities.
/// </summary>
public sealed partial class Interaction : AggregateRoot<InteractionId>
{
    private readonly List<InteractionParticipant> _participants = new();
    private readonly List<InteractionLink> _links = new();

    private Interaction()
    {
        EnsurePersistenceCollectionsInitialized();
    }

    private Interaction(
        InteractionId id,
        InteractionTypeCode type,
        InteractionDirectionCode direction,
        Title subject,
        Body? description,
        DateTime? scheduledAt,
        DateTime? completedAt,
        TimeSpan? duration,
        ActorKindCode createdByKind,
        Guid createdById,
        DateTime createdAtUtc)
    {
        EnsurePersistenceCollectionsInitialized();

        Id = id;
        Type = type;
        Direction = direction;
        Subject = subject;
        Description = description;
        ScheduledAt = scheduledAt?.Kind == DateTimeKind.Utc ? scheduledAt : scheduledAt?.ToUniversalTime();
        CompletedAt = completedAt?.Kind == DateTimeKind.Utc ? completedAt : completedAt?.ToUniversalTime();
        Duration = duration;
        CreatedByKind = createdByKind;
        CreatedById = createdById;
        CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        UpdatedAt = null;

        SyncPersistenceCollectionsFromDomain();
    }

    public override InteractionId Id { get; protected set; }
    public InteractionTypeCode Type { get; private set; }
    public InteractionDirectionCode Direction { get; private set; }
    public Title Subject { get; private set; }
    public Body? Description { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public ActorKindCode CreatedByKind { get; private set; }
    public Guid CreatedById { get; private set; }
    
    public IReadOnlyCollection<InteractionParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<InteractionLink> Links => _links.AsReadOnly();


    // Factory method generating a new InteractionId
    public static Result<Interaction> Create(
        InteractionTypeCode type,
        InteractionDirectionCode direction,
        Title subject,
        Body? description = null,
        DateTime? scheduledAt = null,
        TimeSpan? duration = null,
        ActorKindCode? createdByKind = null,
        Guid? createdById = null,
        DateTime? createdAtUtc = null)
        => Create(
            InteractionId.New(),
            type,
            direction,
            subject,
            description,
            scheduledAt,
            duration,
            createdByKind,
            createdById,
            createdAtUtc);

    // Factory method with explicit InteractionId
    public static Result<Interaction> Create(
        InteractionId id,
        InteractionTypeCode type,
        InteractionDirectionCode direction,
        Title subject,
        Body? description = null,
        DateTime? scheduledAt = null,
        TimeSpan? duration = null,
        ActorKindCode? createdByKind = null,
        Guid? createdById = null,
        DateTime? createdAtUtc = null)
    {
        if (type.IsEmpty)
            return Result<Interaction>.Failure(InteractionErrors.TypeRequired());
        if (direction.IsEmpty)
            return Result<Interaction>.Failure(InteractionErrors.DirectionRequired());
        if (string.IsNullOrWhiteSpace(subject.Value))
            return Result<Interaction>.Failure(InteractionErrors.SubjectRequired());

        var interaction = new Interaction(
            id,
            type,
            direction,
            subject,
            description,
            scheduledAt,
            null, // completedAt is null for new interactions
            duration,
            createdByKind ?? ActorKindCode.User,
            createdById ?? Guid.Empty,
            createdAtUtc ?? DateTime.UtcNow);

        interaction.RaiseEvent(new InteractionCreated(
            interaction.Id,
            interaction.Type,
            interaction.Direction,
            interaction.Subject.Value,
            interaction.Description?.Value,
            interaction.ScheduledAt,
            interaction.CompletedAt,
            interaction.Duration,
            interaction.CreatedByKind,
            interaction.CreatedById,
            interaction.CreatedAt,
            new List<InteractionCreated.ParticipantInfo>(),
            new List<InteractionCreated.LinkInfo>()
        ));

        interaction.SyncPersistenceCollectionsFromDomain();

        return Result<Interaction>.Success(interaction);
    }

    public Result AddParticipant(ActorKindCode participantKind, Guid participantId, ParticipantRoleCode role)
    {
        if (_participants.Any(p => p.ParticipantKind == participantKind && p.ParticipantId == participantId))
            return Result.Failure(InteractionErrors.ParticipantAlreadyExists());

        var participant = InteractionParticipant.Create(Id, participantKind, participantId, role);
        if (participant.IsFailure)
            return Result.Failure(participant.Error);

        _participants.Add(participant.Value);
        SyncPersistenceParticipantsFromDomain();
        RaiseEvent(new InteractionParticipantAdded(
            Id,
            participantKind,
            participantId,
            role
        ));
        return Result.Success();
    }

    public Result RemoveParticipant(ActorKindCode participantKind, Guid participantId)
    {
        var participant = _participants.FirstOrDefault(p => 
            p.ParticipantKind == participantKind && p.ParticipantId == participantId);
        
        if (participant == null)
            return Result.Failure(InteractionErrors.ParticipantNotFound());

        _participants.Remove(participant);
        SyncPersistenceParticipantsFromDomain();
        RaiseEvent(new InteractionParticipantRemoved(
            Id,
            participantKind,
            participantId
        ));
        return Result.Success();
    }

    public Result AddLink(string relatedType, Guid relatedId)
    {
        if (_links.Any(l => l.RelatedType == relatedType && l.RelatedId == relatedId))
            return Result.Failure(InteractionErrors.LinkAlreadyExists());

        var link = InteractionLink.Create(Id, relatedType, relatedId);
        if (link.IsFailure)
            return Result.Failure(link.Error);

        _links.Add(link.Value);
        SyncPersistenceLinksFromDomain();
        return Result.Success();
    }

    public Result RemoveLink(string relatedType, Guid relatedId)
    {
        var link = _links.FirstOrDefault(l => l.RelatedType == relatedType && l.RelatedId == relatedId);
        if (link == null)
            return Result.Failure(InteractionErrors.LinkNotFound());

        _links.Remove(link);
        SyncPersistenceLinksFromDomain();
        return Result.Success();
    }

    public Result Complete(DateTime? completedAt = null, TimeSpan? duration = null)
    {
        if (CompletedAt != null)
            return Result.Failure(InteractionErrors.AlreadyCompleted());

        var completionTime = completedAt ?? DateTime.UtcNow;
        CompletedAt = DateTime.SpecifyKind(completionTime, DateTimeKind.Utc);
        
        if (duration.HasValue)
            Duration = duration;
        else if (ScheduledAt.HasValue)
            Duration = CompletedAt - ScheduledAt;

        return Result.Success();
    }

    public Result UpdateDetails(
        Title? newSubject = null,
        Body? newDescription = null,
        DateTime? newScheduledAt = null)
    {
        var hasChanges = false;

        if (newSubject != null && !string.Equals(newSubject.Value, Subject.Value))
        {
            if (string.IsNullOrWhiteSpace(newSubject.Value.Value))
                return Result.Failure(InteractionErrors.SubjectRequired());
            
            Subject = newSubject.Value;
            hasChanges = true;
        }

        if (newDescription != null && !string.Equals(newDescription.Value, Description?.Value))
        {
            Description = newDescription;
            hasChanges = true;
        }

        if (newScheduledAt != null && newScheduledAt != ScheduledAt)
        {
            ScheduledAt = newScheduledAt?.Kind == DateTimeKind.Utc 
                ? newScheduledAt 
                : newScheduledAt?.ToUniversalTime();
            hasChanges = true;
        }

        if (hasChanges)
        {
                RaiseEvent(new InteractionUpdated(
                Id,
                newSubject?.Value,
                newDescription?.Value,
                newScheduledAt,
                CompletedAt,
                Duration
            ));
        }

        return Result.Success();
    }

    /// <summary>
    /// Applies events to rebuild the aggregate state during event sourcing replay.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    protected override void ApplyEvent(IDomainEvent @event)
    {
        EnsurePersistenceCollectionsInitialized();

        switch (@event)
        {
            case InteractionCreated created:
                Id = created.InteractionId;
                Type = created.Type;
                Direction = created.Direction;
                Subject = Title.TryCreate(created.Subject).Value;
                Description = string.IsNullOrEmpty(created.Description) ? null : Body.TryCreate(created.Description).Value;
                ScheduledAt = created.ScheduledAt;
                CompletedAt = created.CompletedAt;
                Duration = created.Duration;
                CreatedByKind = created.CreatedByKind;
                CreatedById = created.CreatedById;
                CreatedAt = created.CreatedAt;
                SyncPersistenceCollectionsFromDomain();
                break;
                
            case InteractionParticipantAdded participantAdded:
                var newParticipant = InteractionParticipant.Create(Id, participantAdded.ParticipantKind, participantAdded.ParticipantId, participantAdded.Role);
                if (newParticipant.IsSuccess)
                    _participants.Add(newParticipant.Value);
                SyncPersistenceParticipantsFromDomain();
                UpdatedAt = participantAdded.OccurredAtUtc;
                break;
                
            case InteractionParticipantRemoved participantRemoved:
                var participantToRemove = _participants.FirstOrDefault(p => 
                    p.ParticipantKind == participantRemoved.ParticipantKind && 
                    p.ParticipantId == participantRemoved.ParticipantId);
                if (participantToRemove != null)
                    _participants.Remove(participantToRemove);
                SyncPersistenceParticipantsFromDomain();
                UpdatedAt = participantRemoved.OccurredAtUtc;
                break;
                
            case InteractionUpdated updated:
                if (updated.UpdatedSubject != null)
                {
                    var titleResult = Title.TryCreate(updated.UpdatedSubject);
                    if (titleResult.IsSuccess)
                        Subject = titleResult.Value;
                }
                if (updated.UpdatedDescription != null)
                {
                    Description = string.IsNullOrEmpty(updated.UpdatedDescription) ? null : Body.TryCreate(updated.UpdatedDescription).Value;
                }
                if (updated.UpdatedScheduledAt.HasValue)
                    ScheduledAt = updated.UpdatedScheduledAt;
                if (updated.UpdatedCompletedAt.HasValue)
                    CompletedAt = updated.UpdatedCompletedAt;
                if (updated.UpdatedDuration.HasValue)
                    Duration = updated.UpdatedDuration;
                UpdatedAt = updated.OccurredAtUtc;
                break;
                
            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }

    private static class InteractionErrors
    {
        public static Error TypeRequired() => Error.Create("INTERACTION_TYPE_REQUIRED", "Interaction type is required", 400);
        
        public static Error DirectionRequired() => Error.Create("INTERACTION_DIRECTION_REQUIRED", "Interaction direction is required", 400);
        
        public static Error SubjectRequired() => Error.Create("INTERACTION_SUBJECT_REQUIRED", "Interaction subject is required", 400);
        
        public static Error ParticipantAlreadyExists() => Error.Create("INTERACTION_PARTICIPANT_EXISTS", "Participant already exists in this interaction", 409);
        
        public static Error ParticipantNotFound() => Error.Create("INTERACTION_PARTICIPANT_NOT_FOUND", "Participant not found in this interaction", 404);
        
        public static Error LinkAlreadyExists() => Error.Create("INTERACTION_LINK_EXISTS", "Link already exists in this interaction", 409);
        
        public static Error LinkNotFound() => Error.Create("INTERACTION_LINK_NOT_FOUND", "Link not found in this interaction", 404);
        
        public static Error AlreadyCompleted() => Error.Create("INTERACTION_ALREADY_COMPLETED", "Interaction is already completed", 400);
    }
}


