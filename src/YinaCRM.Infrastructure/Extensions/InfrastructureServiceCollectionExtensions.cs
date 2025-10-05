using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using YinaCRM.Infrastructure.Abstractions.Caching;
using YinaCRM.Infrastructure.Abstractions.Messaging;
using YinaCRM.Infrastructure.Abstractions.Notifications;
using YinaCRM.Infrastructure.Abstractions.Persistence;
using YinaCRM.Infrastructure.Abstractions.Search;
using YinaCRM.Infrastructure.Abstractions.Security;
using YinaCRM.Infrastructure.Abstractions.Secrets;
using YinaCRM.Infrastructure.Abstractions.Storage;
using YinaCRM.Infrastructure.Abstractions.Webhooks;
using YinaCRM.Infrastructure.Auth.Auth0;
using YinaCRM.Infrastructure.Caching;
using YinaCRM.Infrastructure.Messaging;
using YinaCRM.Infrastructure.Notifications;
using YinaCRM.Infrastructure.Persistence;
using YinaCRM.Infrastructure.Search;
using YinaCRM.Infrastructure.Secrets;
using YinaCRM.Infrastructure.Security;
using YinaCRM.Infrastructure.Storage;
using YinaCRM.Infrastructure.Webhooks;

namespace YinaCRM.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);

        services.AddAuth0Infrastructure(configuration, "Auth0");

        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection("Infrastructure:Postgres"))
            .ValidateDataAnnotations();

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection("Infrastructure:Redis"))
            .ValidateDataAnnotations();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection("Infrastructure:RabbitMq"))
            .ValidateDataAnnotations();

        services.TryAddSingleton<IDatabaseConnectionFactory, PostgresConnectionFactory>();
        services.TryAddSingleton<IDatabaseMigrator, NoOpDatabaseMigrator>();
        services.TryAddSingleton<IOutboxDispatcher, NoOpOutboxDispatcher>();

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.AllowAdmin = redisOptions.AllowAdmin;
            return ConnectionMultiplexer.Connect(configurationOptions);
        });
        services.TryAddSingleton<IDistributedCache, RedisDistributedCache>();

        services.TryAddSingleton<RabbitMqConnectionProvider>();
        services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.TryAddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();

        services.TryAddSingleton<IFileStorage, InMemoryFileStorage>();
        services.TryAddSingleton<INotificationSender, NoOpNotificationSender>();
        services.TryAddSingleton<ISearchIndexer, NoOpSearchIndexer>();
        services.TryAddSingleton<ISecretStore, InMemorySecretStore>();
        services.TryAddSingleton<ISigningService, HmacSigningService>();
        services.TryAddSingleton<IWebhookDispatcher, HttpWebhookDispatcher>();

        services.AddHttpClient("webhooks")
            .AddPolicyHandler(CreateWebhookRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateWebhookRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
    }
}

