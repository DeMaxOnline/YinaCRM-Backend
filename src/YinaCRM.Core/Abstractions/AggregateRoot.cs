using YinaCRM.Core.Events;

namespace YinaCRM.Core.Abstractions;

public abstract class AggregateRoot<TId> : IAggregateRoot where TId : struct
{
    private readonly List<IDomainEvent> _pendingEvents = new();

    public abstract TId Id { get; protected set; }

    public int Version { get; protected set; }

    public DateTime CreatedAt { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents() => _pendingEvents.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DequeueEvents()
    {
        var events = _pendingEvents.ToArray();
        _pendingEvents.Clear();
        return events;
    }

    public void MarkEventsAsCommitted() => _pendingEvents.Clear();

    protected void RaiseEvent(IDomainEvent @event)
    {
        var isFirstEvent = Version == 0 && _pendingEvents.Count == 0;

        if (@event is DomainEventBase domainEvent)
        {
            @event = domainEvent with { AggregateVersion = Version + 1 };
        }

        _pendingEvents.Add(@event);
        ApplyEvent(@event);
        Version++;

        if (isFirstEvent)
        {
            UpdatedAt = null;
        }
        else if (@event is DomainEventBase enriched)
        {
            UpdatedAt = enriched.OccurredAtUtc;
        }
        else
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    protected virtual void ApplyEvent(IDomainEvent @event)
    {
    }

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event);
            Version++;
        }
    }

    protected void SoftDelete(DateTime? deletedAtUtc = null)
    {
        if (IsDeleted)
        {
            return;
        }

        DeletedAt = (deletedAtUtc ?? DateTime.UtcNow).ToUniversalTime();
        UpdatedAt = DeletedAt;
    }

    protected void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDeletionRetentionExpired(TimeSpan retentionPeriod, DateTime? referenceUtc = null)
    {
        if (!IsDeleted || DeletedAt is null)
        {
            return false;
        }

        var reference = referenceUtc ?? DateTime.UtcNow;
        return DeletedAt.Value <= reference.Add(-retentionPeriod);
    }
}




