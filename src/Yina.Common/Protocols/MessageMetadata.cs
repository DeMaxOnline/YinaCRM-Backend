using System;
using Yina.Common.Diagnostics;
using Yina.Common.Foundation.Clock;

namespace Yina.Common.Protocols;

/// <summary>Correlation-friendly metadata attached to envelopes.</summary>
public readonly record struct MessageMetadata(
    string CorrelationId,
    string? CausationId,
    string? UserId,
    DateTimeOffset TimestampUtc)
{
    public static MessageMetadata Create(
        string? userId = null,
        string? correlationId = null,
        string? causationId = null,
        IClock? clock = null)
    {
        correlationId ??= Correlation.EnsureCorrelationId();
        var ts = (clock ?? SystemClock.Instance).UtcNow;
        return new(correlationId, causationId, userId, ts);
    }
}


