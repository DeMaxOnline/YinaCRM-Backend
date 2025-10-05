using System.Collections.Immutable;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using YinaCRM.Infrastructure.Abstractions.Caching;
using YinaCRM.Infrastructure.Caching;

namespace YinaCRM.Infrastructure.Tests;

public sealed class RedisDistributedCacheTests : IAsyncLifetime
{
    private readonly RedisTestcontainer _container = new TestcontainersBuilder<RedisTestcontainer>()
        .WithDatabase(new RedisTestcontainerConfiguration())
        .Build();

    private IConnectionMultiplexer? _connection;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_container.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    [Fact]
    public async Task SetAndGet_ReturnsPayload()
    {
        var options = new RedisOptions
        {
            ConnectionString = _container.ConnectionString,
            KeyPrefix = "yinacrm-test"
        };

        var cache = new RedisDistributedCache(
            new OptionsWrapper<RedisOptions>(options),
            _connection!,
            NullLogger<RedisDistributedCache>.Instance);

        var payload = new byte[] { 1, 2, 3 };
        var entry = new CacheEntry(
            TenantId: "tenant-1",
            Key: "customer-cache",
            Payload: payload,
            ContentType: "application/octet-stream",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            AbsoluteExpiration: TimeSpan.FromMinutes(5),
            SlidingExpiration: null,
            Tags: ImmutableDictionary<string, string>.Empty);

        var setResult = await cache.SetAsync(entry);
        Assert.True(setResult.IsSuccess, setResult.Error.Message);

        var getResult = await cache.GetAsync(new CacheReadRequest("tenant-1", "customer-cache"));
        Assert.True(getResult.IsSuccess, getResult.Error.Message);
        Assert.True(getResult.Value.Found);
        Assert.Equal(payload, getResult.Value.Payload);
        Assert.Equal("application/octet-stream", getResult.Value.ContentType);
    }

    [Fact]
    public async Task GetAsync_RenewsSlidingExpiration()
    {
        var options = new RedisOptions
        {
            ConnectionString = _container.ConnectionString,
            KeyPrefix = "yinacrm-test",
            DefaultSlidingExpiration = TimeSpan.FromSeconds(5)
        };

        var cache = new RedisDistributedCache(
            new OptionsWrapper<RedisOptions>(options),
            _connection!,
            NullLogger<RedisDistributedCache>.Instance);

        var entry = new CacheEntry(
            TenantId: "tenant-2",
            Key: "sliding-cache",
            Payload: new byte[] { 9, 9, 9 },
            ContentType: "application/octet-stream",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            AbsoluteExpiration: null,
            SlidingExpiration: TimeSpan.FromSeconds(5),
            Tags: ImmutableDictionary<string, string>.Empty);

        var setResult = await cache.SetAsync(entry);
        Assert.True(setResult.IsSuccess, setResult.Error.Message);

        var db = _connection!.GetDatabase();
        var redisKey = $"{options.KeyPrefix}:{entry.TenantId}:{entry.Key}".ToLowerInvariant();
        var ttlBefore = await db.KeyTimeToLiveAsync(redisKey);
        Assert.NotNull(ttlBefore);

        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        var getResult = await cache.GetAsync(new CacheReadRequest(entry.TenantId, entry.Key));
        Assert.True(getResult.IsSuccess, getResult.Error.Message);
        Assert.True(getResult.Value.Found);

        var ttlAfter = await db.KeyTimeToLiveAsync(redisKey);
        Assert.NotNull(ttlAfter);
        Assert.True(ttlAfter > ttlBefore, "Sliding expiration should refresh TTL");
    }
}

