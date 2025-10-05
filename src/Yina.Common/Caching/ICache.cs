using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yina.Common.Caching;

/// <summary>
/// Minimal cache abstraction to decouple consumers from specific cache implementations.
/// </summary>
public interface ICache
{
    /// <summary>Removes an entry from the cache.</summary>
    ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Stores a value in the cache with the given <paramref name="options"/>.</summary>
    ValueTask SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default);

    /// <summary>Attempts to retrieve a cached value.</summary>
    ValueTask<(bool found, T? value)> TryGetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Gets the cached value or adds one using <paramref name="factory"/> when absent.</summary>
    Task<T> GetOrAddAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions options, CancellationToken ct = default);
}
