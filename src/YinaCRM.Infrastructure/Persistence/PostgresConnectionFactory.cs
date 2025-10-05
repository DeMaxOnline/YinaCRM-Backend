using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using YinaCRM.Infrastructure.Abstractions.Persistence;

namespace YinaCRM.Infrastructure.Persistence;

public sealed class PostgresConnectionFactory : IDatabaseConnectionFactory
{
    private readonly IOptionsMonitor<PostgresOptions> _optionsMonitor;
    private readonly ILogger<PostgresConnectionFactory> _logger;

    public PostgresConnectionFactory(
        IOptionsMonitor<PostgresOptions> optionsMonitor,
        ILogger<PostgresConnectionFactory> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        var builder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        if (options.CommandTimeoutSeconds > 0)
        {
            builder.CommandTimeout = options.CommandTimeoutSeconds;
        }

        if (options.MaxPoolSize.HasValue)
        {
            builder.MaxPoolSize = options.MaxPoolSize.Value;
        }

        if (options.MinPoolSize.HasValue)
        {
            builder.MinPoolSize = options.MinPoolSize.Value;
        }

        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Opened PostgreSQL connection to {Host}/{Database}.", builder.Host, builder.Database);
        return connection;
    }
}
