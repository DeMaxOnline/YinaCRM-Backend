using System;
using System.Diagnostics;

namespace Yina.Common.Protocols;

public sealed class MessageEnvelope<TMessage>
    where TMessage : IMessage
{
    public MessageEnvelope(TMessage message, MessageMetadata metadata)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Metadata = metadata;
    }

    public TMessage Message { get; }

    public MessageMetadata Metadata { get; }

    public static MessageEnvelope<TMessage> Create(TMessage message, string? userId = null)
    {
        var meta = MessageMetadata.Create(userId: userId);
        var activityId = Activity.Current?.SpanId.ToString();
        if (!string.IsNullOrWhiteSpace(activityId))
        {
            meta = meta with { CausationId = activityId };
        }

        return new(message, meta);
    }

    public MessageEnvelope<TMessage> With(string? correlationId = null, string? causationId = null, string? userId = null)
    {
        var m = Metadata with
        {
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Metadata.CorrelationId : correlationId!,
            CausationId = string.IsNullOrWhiteSpace(causationId) ? Metadata.CausationId : causationId,
            UserId = string.IsNullOrWhiteSpace(userId) ? Metadata.UserId : userId
        };

        return new(Message, m);
    }
}
