using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Xunit;
using Yina.Common.Abstractions.Results;
using Yina.Common.Protocols;
using YinaCRM.Infrastructure.Abstractions.Messaging;
using YinaCRM.Infrastructure.Messaging;

namespace YinaCRM.Infrastructure.Tests;

public sealed class RabbitMqMessagingTests : IAsyncLifetime
{
    private readonly RabbitMqTestcontainer _container = new TestcontainersBuilder<RabbitMqTestcontainer>()
        .WithRabbitMq(new RabbitMqTestcontainerConfiguration())
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task PublishAndConsume_RoundTripsMessage()
    {
        var options = new RabbitMqOptions
        {
            HostName = _container.Hostname,
            Port = _container.Port,
            UserName = _container.Username,
            Password = _container.Password,
            ExchangeName = "yinacrm.tests",
            QueueName = "yinacrm.tests.queue"
        };

        var optionsMonitor = new StubOptionsMonitor<RabbitMqOptions>(options);
        var provider = new RabbitMqConnectionProvider(optionsMonitor, NullLogger<RabbitMqConnectionProvider>.Instance);
        var publisher = new RabbitMqMessagePublisher(provider, optionsMonitor, NullLogger<RabbitMqMessagePublisher>.Instance);
        var consumer = new RabbitMqMessageConsumer(provider, optionsMonitor, NullLogger<RabbitMqMessageConsumer>.Instance);

        var receivedTcs = new TaskCompletionSource<IncomingMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var consumerTask = consumer.StartAsync(async (message, token) =>
        {
            receivedTcs.TrySetResult(message);
            return await Task.FromResult(Result.Success());
        }, cts.Token);

        var message = new TestMessage("integration-event");
        var envelope = new MessageEnvelope(message, "crm.events.test", "tenant-42", ImmutableDictionary<string, string>.Empty);
        var publishResult = await publisher.PublishAsync(envelope);
        Assert.True(publishResult.IsSuccess, publishResult.Error.Message);

        var received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("crm.events.test", received.Topic);
        Assert.Equal("tenant-42", received.TenantId);

        var payloadJson = Encoding.UTF8.GetString(received.Payload.Span);
        Assert.Contains("integration-event", payloadJson);

        cts.Cancel();
        var stopResult = await consumerTask;
        Assert.True(stopResult.IsSuccess, stopResult.Error.Message);
    }

    private sealed record TestMessage(string Name) : IMessage;
}



