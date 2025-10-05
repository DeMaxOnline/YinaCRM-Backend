using System.Collections.Concurrent;
using System.Text.Json;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Messaging;

namespace YinaCRM.Infrastructure.Messaging;

public sealed class InMemoryMessageBus : IMessagePublisher, IMessageConsumer
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<IncomingMessage>> _messages = new(StringComparer.OrdinalIgnoreCase);

    public Task<Result> PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var queue = _messages.GetOrAdd(envelope.Topic, _ => new ConcurrentQueue<IncomingMessage>());
        var payload = envelope.Message is null
            ? Array.Empty<byte>()
            : JsonSerializer.SerializeToUtf8Bytes(envelope.Message, envelope.Message.GetType());

        queue.Enqueue(new IncomingMessage(
            envelope.Topic,
            envelope.TenantId,
            envelope.Headers,
            new ReadOnlyMemory<byte>(payload)));

        return Task.FromResult(Result.Success());
    }

    public Task<Result> StartAsync(MessageHandler handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        foreach (var queue in _messages.Values)
        {
            while (queue.TryDequeue(out var message))
            {
                var result = handler(message, cancellationToken).GetAwaiter().GetResult();
                if (result.IsFailure)
                {
                    return Task.FromResult(result);
                }
            }
        }

        return Task.FromResult(Result.Success());
    }
}

