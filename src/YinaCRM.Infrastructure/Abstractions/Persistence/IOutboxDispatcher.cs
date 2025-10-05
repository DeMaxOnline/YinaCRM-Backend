using Yina.Common.Abstractions.Results;
using Yina.Common.Protocols;

namespace YinaCRM.Infrastructure.Abstractions.Persistence;

public interface IOutboxDispatcher
{
    Task<Result> DispatchPendingAsync(CancellationToken cancellationToken = default);
}

public sealed record OutboxMessage(
    Guid Id,
    string AggregateType,
    string AggregateId,
    int AggregateVersion,
    IMessage Payload,
    DateTimeOffset OccurredAtUtc,
    string? TenantId);
