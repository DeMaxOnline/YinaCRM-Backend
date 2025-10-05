using System;

namespace Yina.Common.Foundation.Clock;

/// <summary>
/// Abstraction over system time to simplify testing and improve determinism.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current UTC timestamp.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Gets the current UTC timestamp represented as Unix milliseconds.</summary>
    long TimestampUnixMilliseconds { get; }
}
