using System;

namespace Yina.Common.Resilience;

public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;

    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan? AttemptTimeout { get; set; }
}
