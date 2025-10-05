using Yina.Common.Protocols;

namespace YinaCRM.Core.Events;

/// <summary>
/// Base implementation for domain events providing common properties required by DDD.
/// All domain events should inherit from this class to ensure consistency and proper event sourcing support.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <summary>
    /// Initializes a new domain event with required metadata.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate that raised this event</param>
    /// <param name="aggregateType">The type name of the aggregate</param>
    /// <param name="aggregateVersion">The version of the aggregate when event was raised</param>
    protected DomainEventBase(string aggregateId, string aggregateType, int? aggregateVersion = null)
    {
        EventId = Guid.NewGuid();
        OccurredAtUtc = DateTime.UtcNow;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        AggregateVersion = aggregateVersion;
    }

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTime OccurredAtUtc { get; init; }

    /// <inheritdoc />
    public string AggregateType { get; init; }

    /// <inheritdoc />
    public string AggregateId { get; init; }

    /// <inheritdoc />
    public int? AggregateVersion { get; init; }

    /// <inheritdoc />
    public virtual string Name => GetType().Name;
}
