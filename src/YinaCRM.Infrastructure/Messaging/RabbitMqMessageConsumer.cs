using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Messaging;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Messaging;

public sealed class RabbitMqMessageConsumer : IMessageConsumer, IDisposable
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IOptionsMonitor<RabbitMqOptions> _optionsMonitor;
    private readonly ILogger<RabbitMqMessageConsumer> _logger;
    private IModel? _channel;

    public RabbitMqMessageConsumer(
        RabbitMqConnectionProvider connectionProvider,
        IOptionsMonitor<RabbitMqOptions> optionsMonitor,
        ILogger<RabbitMqMessageConsumer> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> StartAsync(MessageHandler handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var options = _optionsMonitor.CurrentValue;
            var connection = _connectionProvider.GetConnection();
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(options.ExchangeName, options.ExchangeType, durable: options.Durable, autoDelete: options.AutoDelete);
            _channel.QueueDeclare(options.QueueName, durable: options.Durable, exclusive: options.Exclusive, autoDelete: options.AutoDelete);
            _channel.QueueBind(options.QueueName, options.ExchangeName, routingKey: options.BindingKey);
            _channel.BasicQos(0, options.PrefetchCount, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (ea.BasicProperties.Headers is { Count: > 0 })
                    {
                        foreach (var header in ea.BasicProperties.Headers)
                        {
                            headers[header.Key] = header.Value switch
                            {
                                byte[] bytes => Encoding.UTF8.GetString(bytes),
                                ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
                                string s => s,
                                object o => o.ToString() ?? string.Empty,
                                _ => string.Empty
                            };
                        }
                    }

                    headers["delivery_tag"] = ea.DeliveryTag.ToString();
                    if (!string.IsNullOrWhiteSpace(ea.BasicProperties.Type))
                    {
                        headers["message_type"] = ea.BasicProperties.Type;
                    }

                    var tenantId = headers.TryGetValue("tenant_id", out var tenant) ? tenant : null;
                    var incoming = new IncomingMessage(
                        topic: ea.RoutingKey,
                        tenantId: tenantId,
                        headers: headers,
                        payload: ea.Body.ToArray());

                    var result = await handler(incoming, cancellationToken).ConfigureAwait(false);
                    if (result.IsSuccess)
                    {
                        _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Message handler failed for routing key {RoutingKey}: {Error}", ea.RoutingKey, result.Error.Message);
                        _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling RabbitMQ message (routing key {RoutingKey}).", ea.RoutingKey);
                    _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            var consumerTag = _channel.BasicConsume(options.QueueName, autoAck: false, consumer: consumer);
            cancellationToken.Register(() =>
            {
                try
                {
                    if (_channel is { IsClosed: false })
                    {
                        _channel.BasicCancel(consumerTag);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cancelling RabbitMQ consumer.");
                }

                tcs.TrySetResult(Result.Success());
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumer.");
            tcs.TrySetResult(Result.Failure(InfrastructureErrors.ExternalDependency("RABBITMQ_CONSUMER_FAILED", ex.Message)));
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        try
        {
            _channel?.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}




