using System;

namespace Yina.Common.Caching;

/// <summary>
/// Additional metadata describing how a cache entry should expire.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>Relative absolute expiration applied from the time the value is cached.</summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>Sliding expiration refreshed whenever the entry is accessed.</summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>Logical size (bytes or units) used for capacity calculations.</summary>
    public long? Size { get; set; }
}
