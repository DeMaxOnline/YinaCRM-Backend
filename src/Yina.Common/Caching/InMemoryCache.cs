using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yina.Common.Caching;

public sealed class InMemoryCache : ICache, IDisposable
{
    private sealed class Entry
    {
        public object? Value { get; set; }

        public DateTimeOffset? AbsoluteExpiry { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public long? Size { get; set; }

        public DateTimeOffset LastAccessUtc { get; set; }
    }

    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    private readonly Timer _cleanupTimer;

    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    private readonly long _maxSize;

    private long _currentSize;

    private bool _disposed;

    public InMemoryCache(long maxSize = 100_000_000)
    {
        _maxSize = maxSize;
        _cleanupTimer = new Timer(
            _ => CleanupExpiredEntries(),
            null,
            _cleanupInterval,
            _cleanupInterval);
    }

    public ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryRemove(key, out var entry))
        {
            if (entry.Size.HasValue)
            {
                Interlocked.Add(ref _currentSize, -entry.Size.Value);
            }

            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }

    public ValueTask SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new Entry
        {
            Value = value,
            AbsoluteExpiry = options.AbsoluteExpirationRelativeToNow is { } abs ? now.Add(abs) : null,
            SlidingExpiration = options.SlidingExpiration,
            Size = options.Size,
            LastAccessUtc = now
        };

        if (options.Size.HasValue)
        {
            EnsureCapacity(options.Size.Value);
            Interlocked.Add(ref _currentSize, options.Size.Value);
        }

        if (_store.TryGetValue(key, out var oldEntry) && oldEntry.Size.HasValue)
        {
            Interlocked.Add(ref _currentSize, -oldEntry.Size.Value);
        }

        _store[key] = entry;
        return ValueTask.CompletedTask;
    }

    public ValueTask<(bool found, T? value)> TryGetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (IsExpired(entry))
            {
                _store.TryRemove(key, out _);
                return new ValueTask<(bool found, T? value)>((false, default));
            }

            Touch(entry);
            if (entry.Value is T t)
            {
                return new ValueTask<(bool found, T? value)>((true, t));
            }

            return new ValueTask<(bool found, T? value)>((false, default));
        }

        return new ValueTask<(bool found, T? value)>((false, default));
    }

    public async Task<T> GetOrAddAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions options, CancellationToken ct = default)
    {
        var (found, val) = await TryGetAsync<T>(key, ct).ConfigureAwait(false);
        if (found)
        {
            return val!;
        }

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            (found, val) = await TryGetAsync<T>(key, ct).ConfigureAwait(false);
            if (found)
            {
                return val!;
            }

            var created = await factory(ct).ConfigureAwait(false);
            await SetAsync(key, created, options, ct).ConfigureAwait(false);
            return created;
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool IsExpired(Entry entry)
    {
        var now = DateTimeOffset.UtcNow;
        if (entry.AbsoluteExpiry is { } abs && now >= abs)
        {
            return true;
        }

        if (entry.SlidingExpiration is { } sliding && now - entry.LastAccessUtc >= sliding)
        {
            return true;
        }

        return false;
    }

    private static void Touch(Entry entry)
    {
        if (entry.SlidingExpiration is not null)
        {
            entry.LastAccessUtc = DateTimeOffset.UtcNow;
        }
    }

    private void CleanupExpiredEntries()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var kvp in _store)
        {
            if (IsExpired(kvp.Value))
            {
                if (_store.TryRemove(kvp.Key, out var entry) && entry.Size.HasValue)
                {
                    Interlocked.Add(ref _currentSize, -entry.Size.Value);
                }
            }
        }

        foreach (var kvp in _locks)
        {
            if (kvp.Value.CurrentCount == 1 && !_store.ContainsKey(kvp.Key))
            {
                if (_locks.TryRemove(kvp.Key, out var semaphore))
                {
                    semaphore.Dispose();
                }
            }
        }
    }

    private void EnsureCapacity(long requiredSize)
    {
        while (Interlocked.Read(ref _currentSize) + requiredSize > _maxSize)
        {
            string? removeKey = null;
            DateTimeOffset oldest = DateTimeOffset.MaxValue;

            foreach (var kvp in _store)
            {
                if (kvp.Value.Size.HasValue && kvp.Value.LastAccessUtc < oldest)
                {
                    oldest = kvp.Value.LastAccessUtc;
                    removeKey = kvp.Key;
                }
            }

            if (removeKey is null)
            {
                break;
            }

            if (_store.TryRemove(removeKey, out var entry) && entry.Size.HasValue)
            {
                Interlocked.Add(ref _currentSize, -entry.Size.Value);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cleanupTimer.Dispose();

        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _locks.Clear();
        _store.Clear();
    }
}
