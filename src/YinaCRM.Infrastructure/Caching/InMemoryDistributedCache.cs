using System.Collections.Concurrent;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Caching;

namespace YinaCRM.Infrastructure.Caching;

public sealed class InMemoryDistributedCache : IDistributedCache
{
    private sealed record CacheItem(
        byte[] Payload,
        string ContentType,
        IReadOnlyDictionary<string, string> Tags,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ExpiresAtUtc,
        TimeSpan? SlidingExpiration);

    private readonly ConcurrentDictionary<string, CacheItem> _entries = new(StringComparer.OrdinalIgnoreCase);

    public Task<Result> SetAsync(CacheEntry entry, CancellationToken cancellationToken = default)
    {
        var expiresAt = entry.AbsoluteExpiration.HasValue
            ? entry.CreatedAtUtc + entry.AbsoluteExpiration
            : (DateTimeOffset?)null;

        var item = new CacheItem(entry.Payload, entry.ContentType, entry.Tags, entry.CreatedAtUtc, expiresAt, entry.SlidingExpiration);
        _entries[BuildKey(entry.TenantId, entry.Key)] = item;
        return Task.FromResult(Result.Success());
    }

    public Task<Result<CacheReadResult>> GetAsync(CacheReadRequest request, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(request.TenantId, request.Key);
        if (!_entries.TryGetValue(key, out var item))
        {
            return Task.FromResult(Result.Success(new CacheReadResult(false, null, null, new Dictionary<string, string>(), null)));
        }

        if (item.ExpiresAtUtc is { } expires && expires <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult(Result.Success(new CacheReadResult(false, null, null, new Dictionary<string, string>(), null)));
        }

        if (item.SlidingExpiration is { } sliding)
        {
            var updated = item with { ExpiresAtUtc = DateTimeOffset.UtcNow + sliding };
            _entries[key] = updated;
            item = updated;
        }

        return Task.FromResult(Result.Success(new CacheReadResult(
            true,
            item.Payload,
            item.ContentType,
            item.Tags,
            item.ExpiresAtUtc)));
    }

    public Task<Result> RemoveAsync(CacheRemoveRequest request, CancellationToken cancellationToken = default)
    {
        _entries.TryRemove(BuildKey(request.TenantId, request.Key), out _);
        return Task.FromResult(Result.Success());
    }

    private static string BuildKey(string tenantId, string key) => $"{tenantId}:{key}".ToLowerInvariant();
}
