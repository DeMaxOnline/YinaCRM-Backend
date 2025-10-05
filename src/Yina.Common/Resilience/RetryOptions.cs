using System;

namespace Yina.Common.Resilience;

/// <summary>Configurable retry settings.</summary>
public sealed class RetryOptions
{
    /// <summary>Total number of attempts, including the first try.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Base delay used by backoff strategies (default 200ms).</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum delay allowed between retries.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Optional timeout applied to each attempt.</summary>
    public TimeSpan? AttemptTimeout { get; set; }
}

