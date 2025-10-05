using System;

namespace Yina.Common.Foundation.Clock;

/// <summary>
/// Production clock using system time. Wraps DateTimeOffset.UtcNow to allow test substitution.
/// </summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    private SystemClock() { }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public long TimestampUnixMilliseconds => UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// Fixed clock for testing scenarios.
/// </summary>
public sealed class FixedClock : IClock
{
    private DateTimeOffset _now;

    public FixedClock(DateTimeOffset now) => _now = now;

    public DateTimeOffset UtcNow => _now;

    public long TimestampUnixMilliseconds => _now.ToUnixTimeMilliseconds();

    public void Advance(TimeSpan by) => _now = _now.Add(by);

    public void Set(DateTimeOffset now) => _now = now;
}
