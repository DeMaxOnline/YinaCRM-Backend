using Yina.Common.Abstractions.Results;
using Yina.Common.Protocols;

namespace YinaCRM.Infrastructure.Abstractions.Messaging;

public interface IMessagePublisher
{
    Task<Result> PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);
}

public sealed record MessageEnvelope(
    IMessage Message,
    string Topic,
    string? TenantId,
    IReadOnlyDictionary<string, string> Headers);
