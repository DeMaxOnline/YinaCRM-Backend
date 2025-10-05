using System;

namespace Yina.Common.Foundation.Clock;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    long TimestampUnixMilliseconds { get; }
}
