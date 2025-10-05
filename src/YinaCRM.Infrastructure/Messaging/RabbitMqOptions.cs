using System;
using System.ComponentModel.DataAnnotations;

namespace YinaCRM.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    [Required]
    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    [Required]
    public string ExchangeName { get; init; } = "yinacrm.events";

    [Required]
    public string QueueName { get; init; } = "yinacrm.events.queue";

    public string ExchangeType { get; init; } = "topic";

    public string BindingKey { get; init; } = "#";

    public bool Durable { get; init; } = true;

    public bool AutoDelete { get; init; }

    public bool Exclusive { get; init; }

    public ushort PrefetchCount { get; init; } = 10;

    public bool UseSsl { get; init; }
}



