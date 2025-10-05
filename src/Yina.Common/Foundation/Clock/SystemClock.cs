using System;

namespace Yina.Common.Foundation.Clock;

/// <summary>
/// Production clock that surfaces the system UTC time.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>A shared instance for general use.</summary>
    public static readonly SystemClock Instance = new();

    private SystemClock() { }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public long TimestampUnixMilliseconds => UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// Deterministic clock that can be advanced manually, ideal for testing.
/// </summary>
public sealed class FixedClock : IClock
{
    private DateTimeOffset _now;

    public FixedClock(DateTimeOffset now) => _now = now;

    /// <inheritdoc />
    public DateTimeOffset UtcNow => _now;

    /// <inheritdoc />
    public long TimestampUnixMilliseconds => _now.ToUnixTimeMilliseconds();

    /// <summary>Advances the clock by the specified <paramref name="by"/> duration.</summary>
    public void Advance(TimeSpan by) => _now = _now.Add(by);

    /// <summary>Sets the clock to the supplied <paramref name="now"/> timestamp.</summary>
    public void Set(DateTimeOffset now) => _now = now;
}
