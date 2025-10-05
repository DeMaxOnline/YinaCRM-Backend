using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.Interaction.VOs;

namespace YinaCRM.Core.Entities.Interaction;

/// <summary>
/// Immutable value object representing a link between an interaction and another entity.
/// Composite key: InteractionId + RelatedType + RelatedId
/// </summary>
public sealed class InteractionLink : IEquatable<InteractionLink>
{
    private InteractionLink(
        InteractionId interactionId,
        string relatedType,
        Guid relatedId)
    {
        InteractionId = interactionId;
        RelatedType = relatedType;
        RelatedId = relatedId;
    }

    public InteractionId InteractionId { get; }
    public string RelatedType { get; }
    public Guid RelatedId { get; }

    /// <summary>
    /// Creates a new InteractionLink with validation
    /// </summary>
    public static Result<InteractionLink> Create(
        InteractionId interactionId,
        string relatedType,
        Guid relatedId)
    {
        if (string.IsNullOrWhiteSpace(relatedType))
            return Result<InteractionLink>.Failure(InteractionLinkErrors.InvalidRelatedType());

        if (relatedId == Guid.Empty)
            return Result<InteractionLink>.Failure(InteractionLinkErrors.InvalidRelatedId());

        return Result<InteractionLink>.Success(new InteractionLink(
            interactionId,
            relatedType.Trim(),
            relatedId));
    }

    /// <summary>
    /// Checks if this link matches the given type and ID
    /// </summary>
    public bool Matches(string type, Guid id)
    {
        return string.Equals(RelatedType, type, StringComparison.OrdinalIgnoreCase) && RelatedId == id;
    }

    public bool Equals(InteractionLink? other)
    {
        if (other is null) return false;
        return InteractionId.Equals(other.InteractionId) &&
               string.Equals(RelatedType, other.RelatedType, StringComparison.OrdinalIgnoreCase) &&
               RelatedId == other.RelatedId;
    }

    public override bool Equals(object? obj) => obj is InteractionLink other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        InteractionId, 
        RelatedType?.ToUpperInvariant(), 
        RelatedId);

    public override string ToString() => $"{RelatedType}:{RelatedId}";
}

internal static class InteractionLinkErrors
{
    public static Error InvalidRelatedType() => Error.Create("INTERACTION_LINK_TYPE_INVALID", "Related type cannot be empty", 400);

    public static Error InvalidRelatedId() => Error.Create("INTERACTION_LINK_ID_INVALID", "Related ID cannot be empty", 400);
}


