using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Caching;

public interface IDistributedCache
{
    Task<Result> SetAsync(CacheEntry entry, CancellationToken cancellationToken = default);

    Task<Result<CacheReadResult>> GetAsync(CacheReadRequest request, CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(CacheRemoveRequest request, CancellationToken cancellationToken = default);
}

public sealed record CacheEntry(
    string TenantId,
    string Key,
    byte[] Payload,
    string ContentType,
    DateTimeOffset CreatedAtUtc,
    TimeSpan? AbsoluteExpiration,
    TimeSpan? SlidingExpiration,
    IReadOnlyDictionary<string, string> Tags);

public sealed record CacheReadRequest(
    string TenantId,
    string Key);

public sealed record CacheReadResult(
    bool Found,
    byte[]? Payload,
    string? ContentType,
    IReadOnlyDictionary<string, string> Tags,
    DateTimeOffset? ExpiresAtUtc);

public sealed record CacheRemoveRequest(
    string TenantId,
    string Key);
