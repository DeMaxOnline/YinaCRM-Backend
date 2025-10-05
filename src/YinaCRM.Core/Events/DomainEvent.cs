namespace YinaCRM.Core.Events;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(string aggregateType, string aggregateId, int? aggregateVersion = null)
    {
        EventId = Guid.NewGuid();
        OccurredAtUtc = DateTime.UtcNow;
        AggregateType = aggregateType ?? throw new ArgumentNullException(nameof(aggregateType));
        AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
        AggregateVersion = aggregateVersion;
    }

    public Guid EventId { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public string AggregateType { get; init; }

    public string AggregateId { get; init; }

    public int? AggregateVersion { get; init; }

    public abstract string Name { get; }
}
