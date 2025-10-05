using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Caching;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Caching;

public sealed class RedisDistributedCache : IDistributedCache, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RedisOptions _options;
    private readonly ILogger<RedisDistributedCache> _logger;
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;

    public RedisDistributedCache(
        IOptions<RedisOptions> options,
        IConnectionMultiplexer connection,
        ILogger<RedisDistributedCache> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _database = _connection.GetDatabase();
    }

    public async Task<Result> SetAsync(CacheEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var absoluteExpiration = entry.AbsoluteExpiration.HasValue ? now + entry.AbsoluteExpiration.Value : (DateTimeOffset?)null;
            var slidingExpiration = entry.SlidingExpiration ?? _options.DefaultSlidingExpiration;
            var ttl = DetermineTimeToLive(entry.AbsoluteExpiration, slidingExpiration, _options.DefaultAbsoluteExpiration, _options.DefaultSlidingExpiration);

            if (ttl is null)
            {
                _logger.LogWarning("Redis cache entry missing expiration. Falling back to default 1 hour TTL.");
                ttl = TimeSpan.FromHours(1);
            }

            var record = new RedisCacheRecord(
                entry.ContentType,
                entry.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                entry.Payload,
                entry.CreatedAtUtc,
                absoluteExpiration,
                slidingExpiration);

            var redisValue = JsonSerializer.Serialize(record, SerializerOptions);
            var key = BuildKey(entry.TenantId, entry.Key);
            await _database.StringSetAsync(key, redisValue, ttl, When.Always, CommandFlags.None).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Redis cache entry for {Tenant}/{Key}.", entry.TenantId, entry.Key);
            return Result.Failure(InfrastructureErrors.ExternalDependency("REDIS_CACHE_SET_FAILED", ex.Message));
        }
    }

    public async Task<Result<CacheReadResult>> GetAsync(CacheReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var key = BuildKey(request.TenantId, request.Key);
            var value = await _database.StringGetAsync(key).ConfigureAwait(false);
            if (!value.HasValue)
            {
                return Result.Success(new CacheReadResult(false, null, null, new Dictionary<string, string>(), null));
            }

            var record = JsonSerializer.Deserialize<RedisCacheRecord>(value!, SerializerOptions);
            if (record is null)
            {
                await _database.KeyDeleteAsync(key).ConfigureAwait(false);
                return Result.Success(new CacheReadResult(false, null, null, new Dictionary<string, string>(), null));
            }

            if (record.SlidingExpiration is not null)
            {
                await _database.KeyExpireAsync(key, record.SlidingExpiration, CommandFlags.FireAndForget).ConfigureAwait(false);
            }

            var expiresAt = await _database.KeyTimeToLiveAsync(key).ConfigureAwait(false);
            DateTimeOffset? expiresAtUtc = null;
            if (expiresAt is { })
            {
                expiresAtUtc = DateTimeOffset.UtcNow + expiresAt.Value;
            }

            return Result.Success(new CacheReadResult(
                true,
                record.Payload,
                record.ContentType,
                record.Tags,
                record.AbsoluteExpirationUtc ?? expiresAtUtc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Redis cache entry for {Tenant}/{Key}.", request.TenantId, request.Key);
            return Result.Failure<CacheReadResult>(InfrastructureErrors.ExternalDependency("REDIS_CACHE_GET_FAILED", ex.Message));
        }
    }

    public async Task<Result> RemoveAsync(CacheRemoveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var key = BuildKey(request.TenantId, request.Key);
            await _database.KeyDeleteAsync(key).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Redis cache entry for {Tenant}/{Key}.", request.TenantId, request.Key);
            return Result.Failure(InfrastructureErrors.ExternalDependency("REDIS_CACHE_REMOVE_FAILED", ex.Message));
        }
    }

    private static TimeSpan? DetermineTimeToLive(TimeSpan? absolute, TimeSpan? sliding, TimeSpan? defaultAbsolute, TimeSpan? defaultSliding)
    {
        var candidates = new List<TimeSpan?> { absolute, sliding, defaultAbsolute, defaultSliding };
        var valid = candidates.Where(t => t.HasValue && t.Value > TimeSpan.Zero).Select(t => t!.Value).ToList();
        if (valid.Count == 0)
        {
            return null;
        }

        return valid.Min();
    }

    private string BuildKey(string tenantId, string key)
        => $"{_options.KeyPrefix}:{tenantId}:{key}".ToLowerInvariant();

    public void Dispose()
    {
        // connection multiplexer owned by DI container
    }

    private sealed record RedisCacheRecord(
        string ContentType,
        Dictionary<string, string> Tags,
        byte[] Payload,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? AbsoluteExpirationUtc,
        TimeSpan? SlidingExpiration);
}




