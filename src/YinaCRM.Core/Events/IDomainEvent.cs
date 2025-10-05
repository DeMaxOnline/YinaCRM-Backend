using Yina.Common.Protocols;

namespace YinaCRM.Core.Events;

public interface IDomainEvent : IMessage
{
    Guid EventId { get; }

    DateTime OccurredAtUtc { get; }

    string AggregateType { get; }

    string AggregateId { get; }

    int? AggregateVersion { get; }
}
