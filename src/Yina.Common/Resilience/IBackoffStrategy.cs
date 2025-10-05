using System;

namespace Yina.Common.Resilience;

/// <summary>Provides delay intervals between retry attempts.</summary>
public interface IBackoffStrategy
{
    TimeSpan GetDelay(int attemptNumber);
}

/// <summary>
/// Full jitter exponential backoff: random(0, base * 2^(n-1)), capped by max.
/// </summary>
public sealed class JitteredExponentialBackoffStrategy : IBackoffStrategy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly Random _random = Random.Shared;

    public JitteredExponentialBackoffStrategy(TimeSpan baseDelay, TimeSpan maxDelay)
    {
        if (baseDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDelay));
        }

        if (maxDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelay));
        }

        _baseDelay = baseDelay;
        _maxDelay = maxDelay;
    }

    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber < 1)
        {
            attemptNumber = 1;
        }

        var expo = Math.Min(_baseDelay.TotalMilliseconds * Math.Pow(2, attemptNumber - 1), _maxDelay.TotalMilliseconds);
        var jitter = _random.NextDouble() * expo;
        return TimeSpan.FromMilliseconds(jitter);
    }
}



