using System.Linq;
using Yina.Common.Foundation.Ids;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Events;

namespace YinaCRM.Core.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void RaiseEvent_TracksEventAndUpdatesState()
    {
        var aggregate = new FakeAggregate();
        var evt = new FakeEvent(aggregate.Id.ToString());

        aggregate.Trigger(evt);

        var pending = aggregate.GetUncommittedEvents();
        var stored = Assert.IsType<FakeEvent>(Assert.Single(pending));
        Assert.NotSame(evt, stored);
        Assert.Equal(1, stored.AggregateVersion);
        Assert.Equal(evt.AggregateId, stored.AggregateId);
        Assert.Equal(1, aggregate.Version);
        Assert.NotNull(aggregate.UpdatedAt);

        var dequeued = aggregate.DequeueEvents();
        Assert.Equal(stored, Assert.Single(dequeued));
        Assert.Empty(aggregate.GetUncommittedEvents());

        aggregate.MarkEventsAsCommitted();
        Assert.Empty(aggregate.GetUncommittedEvents());
    }

    [Fact]
    public void SoftDeleteAndRestore_UpdateFlagsAndRetention()
    {
        var aggregate = new FakeAggregate();
        aggregate.ForceSoftDelete();

        Assert.True(aggregate.IsDeleted);
        Assert.NotNull(aggregate.DeletedAt);
        Assert.False(aggregate.IsDeletionRetentionExpired(TimeSpan.FromDays(30)));

        var reference = aggregate.DeletedAt!.Value.AddDays(31);
        Assert.True(aggregate.IsDeletionRetentionExpired(TimeSpan.FromDays(30), reference));

        aggregate.ForceRestore();
        Assert.False(aggregate.IsDeleted);
        Assert.Null(aggregate.DeletedAt);
    }

    [Fact]
    public void LoadFromHistory_ReplaysEvents()
    {
        var aggregate = new FakeAggregate();
        var history = new[]
        {
            new FakeEvent(aggregate.Id.ToString()) { AggregateVersion = 1 },
            new FakeEvent(aggregate.Id.ToString()) { AggregateVersion = 2 }
        };

        aggregate.Replay(history);

        Assert.Equal(2, aggregate.Version);
        Assert.Equal(2, aggregate.AppliedEvents.Count);
    }

    private sealed record FakeEvent(string AggregateId)
        : DomainEventBase(AggregateId, nameof(FakeAggregate))
    {
        public override string Name => nameof(FakeEvent);
    }

    private sealed class FakeAggregate : AggregateRoot<StrongId<FakeAggregateIdTag>>
    {
        public override StrongId<FakeAggregateIdTag> Id { get; protected set; }

        public List<IDomainEvent> AppliedEvents { get; } = new();

        public FakeAggregate()
        {
            Id = StrongId<FakeAggregateIdTag>.New();
            CreatedAt = DateTime.UtcNow;
        }

        public void Trigger(IDomainEvent evt) => RaiseEvent(evt);

        public void ForceSoftDelete() => SoftDelete();

        public void ForceRestore() => Restore();

        public void Replay(IEnumerable<IDomainEvent> history) => LoadFromHistory(history);

        protected override void ApplyEvent(IDomainEvent @event)
        {
            AppliedEvents.Add(@event);
        }
    }

    private readonly struct FakeAggregateIdTag { }
}



