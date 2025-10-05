using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yina.Common.Caching;

public interface ICache
{
    ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default);

    ValueTask SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default);

    ValueTask<(bool found, T? value)> TryGetAsync<T>(string key, CancellationToken ct = default);

    Task<T> GetOrAddAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions options, CancellationToken ct = default);
}
