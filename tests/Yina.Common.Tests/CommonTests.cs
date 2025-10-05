using System.Net;
using System.Text.Json;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using Yina.Common.Abstractions.Results.Extensions;
using Yina.Common.Caching;
using Yina.Common.Foundation.Ids;
using Yina.Common.Protocols;
using Yina.Common.Resilience;
using Yina.Common.Serialization;

namespace Yina.Common.Tests;

public sealed class ResultTests
{
    [Fact]
    public void DefaultResult_IsSuccessAndSafe()
    {
        Result result = default;
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void DefaultGenericResult_IsSuccessAndProvidesDefaultValue()
    {
        Result<int> result = default;
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Then_ExecutesBinderOnlyOnSuccess()
    {
        var binderCalls = 0;

        var success = Result.Success()
            .Then(() =>
            {
                binderCalls++;
                return Result.Success();
            });

        Assert.True(success.IsSuccess);
        Assert.Equal(1, binderCalls);

        var failure = Result.Failure(Errors.Failure("FAIL", "Failure"))
            .Then(() =>
            {
                binderCalls++;
                return Result.Success();
            });

        Assert.True(failure.IsFailure);
        Assert.Equal(1, binderCalls);
    }

    [Fact]
    public void WhereProducesFailureWhenPredicateFails()
    {
        var ok = Result.Success(10).Where(x => x % 2 == 0);
        Assert.True(ok.IsSuccess);

        var failed = Result.Success(11).Where(x => x % 2 == 0);
        Assert.True(failed.IsFailure);
        Assert.Equal("PREDICATE_FAILED", failed.Error.Code);
    }
}

public sealed class StrongIdJsonTests
{
    private readonly JsonSerializerOptions _options = JsonDefaults.Create();

    private sealed record ClientIdTag;

    [Fact]
    public void StrongId_RoundtripsThroughJson()
    {
        var id = StrongId<ClientIdTag>.New();
        var json = JsonSerializer.Serialize(id, _options);
        var roundtrip = JsonSerializer.Deserialize<StrongId<ClientIdTag>>(json, _options);
        Assert.Equal(id, roundtrip);
    }

    [Fact]
    public void NullableStrongId_RoundtripsThroughJson()
    {
        StrongId<ClientIdTag>? id = StrongId<ClientIdTag>.New();
        var json = JsonSerializer.Serialize(id, _options);
        var roundtrip = JsonSerializer.Deserialize<StrongId<ClientIdTag>?>(json, _options);
        Assert.Equal(id, roundtrip);

        id = null;
        json = JsonSerializer.Serialize(id, _options);
        Assert.Equal("null", json);
    }

    [Fact]
    public void InvalidPayloadThrows()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StrongId<ClientIdTag>>("\"not-a-guid\"", _options));
    }
}

public sealed class JsonDefaultsTests
{
    [Fact]
    public void Create_WithIndentation_ProducesIndentedOutput()
    {
        var options = JsonDefaults.Create(indented: true);
        var json = JsonSerializer.Serialize(new { Value = 1 }, options);
        Assert.Contains("\n", json);
    }
}

public sealed class ErrorTests
{
    [Fact]
    public void NormalizesErrorCodes()
    {
        var error = Errors.Validation("customer missing", "bad", field: "name");
        Assert.Equal("CUSTOMER_MISSING", error.Code);
        Assert.Equal("name", error.Field);
    }

    [Fact]
    public void FromExceptionHidesDetailsInRelease()
    {
        var error = Errors.FromException(new InvalidOperationException("secret"));
        Assert.Equal("UNHANDLED_EXCEPTION", error.Code);
        Assert.Equal("An unexpected error occurred.", error.Message);
    }
}

public sealed class RetryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_RetriesTransientExceptions()
    {
        var attempts = 0;

        await RetryExecutor.ExecuteAsync(
            action: _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new HttpRequestException("fail", null, HttpStatusCode.ServiceUnavailable);
                }

                return Task.CompletedTask;
            },
            options: new RetryOptions
            {
                MaxAttempts = 3,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(1)
            },
            backoff: new ZeroBackoffStrategy());

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ResultStopsOnNonRetryableError()
    {
        var attempts = 0;

        var result = await RetryExecutor.ExecuteAsync(
            action: _ =>
            {
                attempts++;
                return Task.FromResult(Result<int>.Failure(Errors.Validation("INVALID", "nope")));
            },
            options: new RetryOptions
            {
                MaxAttempts = 5,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                MaxDelay = TimeSpan.FromMilliseconds(1)
            },
            backoff: new ZeroBackoffStrategy());

        Assert.True(result.IsFailure);
        Assert.InRange(attempts, 1, 2);
    }

    [Fact]
    public async Task AttemptTimeoutCancelsBetweenRetries()
    {
        var attempts = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await RetryExecutor.ExecuteAsync(
                async ct =>
                {
                    attempts++;
                    await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
                },
                new RetryOptions
                {
                    MaxAttempts = 2,
                    AttemptTimeout = TimeSpan.FromMilliseconds(10),
                    BaseDelay = TimeSpan.FromMilliseconds(1),
                    MaxDelay = TimeSpan.FromMilliseconds(1)
                });
        });

        Assert.InRange(attempts, 1, 2);
    }

    private sealed class ZeroBackoffStrategy : IBackoffStrategy
    {
        public TimeSpan GetDelay(int attemptNumber) => TimeSpan.Zero;
    }
}

public sealed class InMemoryCacheTests
{
    [Fact]
    public async Task GetOrAddAsync_CachesValue()
    {
        var cache = new InMemoryCache();
        var calls = 0;

        var value1 = await cache.GetOrAddAsync(
            "key",
            _ =>
            {
                calls++;
                return Task.FromResult("value");
            },
            new CacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });

        var value2 = await cache.GetOrAddAsync(
            "key",
            _ =>
            {
                calls++;
                return Task.FromResult("value-2");
            },
            new CacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });

        Assert.Equal("value", value1);
        Assert.Equal("value", value2);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task EnsureCapacity_BoundedSweepPreventsInfiniteLoop()
    {
        var cache = new InMemoryCache(maxSize: 5);
        var options = new CacheEntryOptions { Size = 1 };

        for (var i = 0; i < 100; i++)
        {
            await cache.SetAsync("stale", i, options);
        }

        await cache.SetAsync("fresh", "value", options);

        var (found, value) = await cache.TryGetAsync<string>("fresh");
        Assert.True(found);
        Assert.Equal("value", value);
    }

    [Fact]
    public async Task AbsoluteExpirationExpiresEntry()
    {
        var cache = new InMemoryCache();
        await cache.SetAsync("expiring", "value", new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(10) });
        await Task.Delay(30);
        var (found, _) = await cache.TryGetAsync<string>("expiring");
        Assert.False(found);
    }

    [Fact]
    public async Task TypeMismatchTreatsAsMiss()
    {
        var cache = new InMemoryCache();
        await cache.SetAsync("number", 5, new CacheEntryOptions());
        var (found, value) = await cache.TryGetAsync<string>("number");
        Assert.False(found);
        Assert.Null(value);
    }
}

public sealed class CacheConcurrencyTests
{
    [Fact]
    public async Task GetOrAddAsync_IsSingleFlight()
    {
        var cache = new InMemoryCache();
        var calls = 0;

        async Task<string> Factory(CancellationToken _)
        {
            await Task.Delay(10);
            Interlocked.Increment(ref calls);
            return "value";
        }

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => cache.GetOrAddAsync("once", Factory, new CacheEntryOptions()));

        await Task.WhenAll(tasks);

        Assert.Equal(1, calls);
    }
}

public sealed class ProtocolMetadataTests
{
    [Fact]
    public void EnvelopeAddsActivityCorrelation()
    {
        var message = new TestMessage();
        var envelope = MessageEnvelope<TestMessage>.Create(message, userId: "user-1");
        Assert.Equal(message.Name, envelope.Message.Name);
        Assert.Equal("user-1", envelope.Metadata.UserId);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Metadata.CorrelationId));
    }

    private sealed class TestMessage : IMessage
    {
        public string Name => "test-message";
    }
}



public sealed class QueryMetadataTests
{
    private sealed class SampleQuery : IQuery<string>
    {
        public string Name => "sample-query";
    }

    [Fact]
    public void QueryImplementsIMessage()
    {
        IQuery<string> query = new SampleQuery();
        Assert.Equal("sample-query", query.Name);
        Assert.IsAssignableFrom<IMessage>(query);
    }
}








