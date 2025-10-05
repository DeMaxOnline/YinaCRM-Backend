using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Messaging;

public interface IMessageConsumer
{
    Task<Result> StartAsync(MessageHandler handler, CancellationToken cancellationToken = default);
}

public delegate Task<Result> MessageHandler(IncomingMessage message, CancellationToken cancellationToken);

public sealed record IncomingMessage(
    string Topic,
    string? TenantId,
    IReadOnlyDictionary<string, string> Headers,
    ReadOnlyMemory<byte> Payload);
