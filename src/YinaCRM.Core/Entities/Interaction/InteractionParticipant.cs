using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.Interaction.VOs;
using YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

namespace YinaCRM.Core.Entities.Interaction;

/// <summary>
/// Immutable value object representing a participant in an interaction.
/// Composite key: InteractionId + ParticipantKind + ParticipantId
/// </summary>
public sealed class InteractionParticipant : IEquatable<InteractionParticipant>
{
    private InteractionParticipant(
        InteractionId interactionId,
        ActorKindCode participantKind,
        Guid participantId,
        ParticipantRoleCode role)
    {
        InteractionId = interactionId;
        ParticipantKind = participantKind;
        ParticipantId = participantId;
        Role = role;
    }

    public InteractionId InteractionId { get; }
    public ActorKindCode ParticipantKind { get; }
    public Guid ParticipantId { get; }
    public ParticipantRoleCode Role { get; }

    /// <summary>
    /// Creates a new InteractionParticipant with validation
    /// </summary>
    public static Result<InteractionParticipant> Create(
        InteractionId interactionId,
        ActorKindCode participantKind,
        Guid participantId,
        ParticipantRoleCode role)
    {
        if (participantId == Guid.Empty)
            return Result<InteractionParticipant>.Failure(InteractionParticipantErrors.InvalidParticipantId());

        if (participantKind.Value == string.Empty)
            return Result<InteractionParticipant>.Failure(InteractionParticipantErrors.ParticipantKindRequired());

        if (role.IsEmpty)
            return Result<InteractionParticipant>.Failure(InteractionParticipantErrors.RoleRequired());

        return Result<InteractionParticipant>.Success(new InteractionParticipant(
            interactionId,
            participantKind,
            participantId,
            role));
    }

    /// <summary>
    /// Creates a new InteractionParticipant with updated role while preserving other properties
    /// </summary>
    public Result<InteractionParticipant> WithRole(ParticipantRoleCode newRole)
    {
        if (newRole.IsEmpty)
            return Result<InteractionParticipant>.Failure(InteractionParticipantErrors.RoleRequired());

        return Result<InteractionParticipant>.Success(new InteractionParticipant(
            InteractionId,
            ParticipantKind,
            ParticipantId,
            newRole));
    }

    /// <summary>
    /// Checks if this participant matches the given kind and ID
    /// </summary>
    public bool Matches(ActorKindCode kind, Guid id)
    {
        return ParticipantKind.Equals(kind) && ParticipantId == id;
    }

    public bool Equals(InteractionParticipant? other)
    {
        if (other is null) return false;
        return InteractionId.Equals(other.InteractionId) &&
               ParticipantKind.Equals(other.ParticipantKind) &&
               ParticipantId == other.ParticipantId;
    }

    public override bool Equals(object? obj) => obj is InteractionParticipant other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(InteractionId, ParticipantKind, ParticipantId);

    public override string ToString() => $"{ParticipantKind}:{ParticipantId} as {Role}";
}

internal static class InteractionParticipantErrors
{
    public static Error InvalidParticipantId() => Error.Create("INTERACTION_PARTICIPANT_ID_INVALID", "Participant ID cannot be empty", 400);

    public static Error ParticipantKindRequired() => Error.Create("INTERACTION_PARTICIPANT_KIND_REQUIRED", "Participant kind is required", 400);

    public static Error RoleRequired() => Error.Create("INTERACTION_PARTICIPANT_ROLE_REQUIRED", "Participant role is required", 400);
}

