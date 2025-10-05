using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Yina.Common.Abstractions.Results;
using Yina.Common.Serialization;
using Yina.Common.Protocols;
using YinaCRM.Infrastructure.Abstractions.Messaging;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher : IMessagePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonDefaults.Create();

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IOptionsMonitor<RabbitMqOptions> _optionsMonitor;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;

    public RabbitMqMessagePublisher(
        RabbitMqConnectionProvider connectionProvider,
        IOptionsMonitor<RabbitMqOptions> optionsMonitor,
        ILogger<RabbitMqMessagePublisher> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        try
        {
            var options = _optionsMonitor.CurrentValue;
            var connection = _connectionProvider.GetConnection();

            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(options.ExchangeName, options.ExchangeType, durable: options.Durable, autoDelete: options.AutoDelete);

            var payload = JsonSerializer.SerializeToUtf8Bytes(envelope.Message, envelope.Message.GetType(), SerializerOptions);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = envelope.Message.Name;
            properties.Headers = new Dictionary<string, object?>
            {
                ["message_type"] = envelope.Message.Name,
                ["content_type"] = envelope.Message.GetType().FullName ?? envelope.Message.Name
            };

            if (!string.IsNullOrWhiteSpace(envelope.TenantId))
            {
                properties.Headers["tenant_id"] = envelope.TenantId;
            }

            foreach (var header in envelope.Headers)
            {
                properties.Headers[header.Key] = header.Value;
            }

            await Task.Run(() => channel.BasicPublish(options.ExchangeName, envelope.Topic, mandatory: false, basicProperties: properties, body: payload), cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Published message {MessageType} to {Exchange}/{RoutingKey} (tenant: {Tenant}).", envelope.Message.Name, options.ExchangeName, envelope.Topic, envelope.TenantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageType} to RabbitMQ.", envelope.Message.Name);
            return Result.Failure(InfrastructureErrors.ExternalDependency("RABBITMQ_PUBLISH_FAILED", ex.Message));
        }
    }
}




