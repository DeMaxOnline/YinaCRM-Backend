using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace YinaCRM.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IDisposable
{
    private readonly IOptionsMonitor<RabbitMqOptions> _optionsMonitor;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;
    private IConnection? _connection;
    private readonly object _sync = new();

    public RabbitMqConnectionProvider(IOptionsMonitor<RabbitMqOptions> optionsMonitor, ILogger<RabbitMqConnectionProvider> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_sync)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            var options = _optionsMonitor.CurrentValue;
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                DispatchConsumersAsync = true
            };

            if (options.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = options.HostName
                };
            }

            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port} (vhost {VHost}).", factory.HostName, factory.Port, factory.VirtualHost);
            _connection = factory.CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // ignore dispose failures
        }
    }
}
