using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Security;
using YinaCRM.Infrastructure.Abstractions.Webhooks;
using YinaCRM.Infrastructure.Webhooks;

namespace YinaCRM.Infrastructure.Tests;

public class HttpWebhookDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_DeliversWebhook_WhenSuccessful()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var dispatcher = new HttpWebhookDispatcher(
            new StubHttpClientFactory(client),
            new StubSigningService(Result.Success("signature")),
            NullLogger<HttpWebhookDispatcher>.Instance,
            TimeProvider.System);

        var request = new WebhookDispatchRequest(
            Endpoint: "https://endpoint",
            Secret: "secret",
            TenantId: "tenant",
            EventType: "event",
            Payload: "{}",
            Headers: new Dictionary<string, string>(),
            Timeout: TimeSpan.FromSeconds(5),
            MaxAttempts: 3);

        var result = await dispatcher.DispatchAsync(request);

        Assert.True(result.Value.Delivered);
        Assert.Equal(1, result.Value.Attempts);
    }

    [Fact]
    public async Task DispatchAsync_ReturnsFailures_WhenAttemptsExhausted()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var client = new HttpClient(handler);
        var dispatcher = new HttpWebhookDispatcher(
            new StubHttpClientFactory(client),
            new StubSigningService(Result.Success("signature")),
            NullLogger<HttpWebhookDispatcher>.Instance,
            new FakeTimeProvider());

        var request = new WebhookDispatchRequest(
            Endpoint: "https://endpoint",
            Secret: "secret",
            TenantId: "tenant",
            EventType: "event",
            Payload: "{}",
            Headers: new Dictionary<string, string>(),
            Timeout: TimeSpan.FromSeconds(1),
            MaxAttempts: 2);

        var result = await dispatcher.DispatchAsync(request);

        Assert.False(result.Value.Delivered);
        Assert.Equal(2, result.Value.Attempts);
        Assert.NotEmpty(result.Value.FailureReasons);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubSigningService : ISigningService
    {
        private readonly Result<string> _signResult;

        public StubSigningService(Result<string> signResult)
        {
            _signResult = signResult;
        }

        public Task<Result<string>> SignAsync(SignRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_signResult);

        public Task<Result<bool>> VerifyAsync(VerifySignatureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success(true));
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now += TimeSpan.FromMilliseconds(10);
    }
}
