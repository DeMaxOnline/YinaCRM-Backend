using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yina.Common.Abstractions.Results;
using Yina.Common.Protocols;
using Yina.Common.Serialization;
using YinaCRM.Infrastructure.Abstractions.Messaging;
using YinaCRM.Infrastructure.Abstractions.Persistence;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Persistence;

public sealed class PostgresOutboxDispatcher : IOutboxDispatcher
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonDefaults.Create();

    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IMessagePublisher _publisher;
    private readonly IOptions<PostgresOutboxOptions> _options;
    private readonly ILogger<PostgresOutboxDispatcher> _logger;
    private readonly TimeProvider _timeProvider;

    public PostgresOutboxDispatcher(
        IDatabaseConnectionFactory connectionFactory,
        IMessagePublisher publisher,
        IOptions<PostgresOutboxOptions> options,
        ILogger<PostgresOutboxDispatcher> logger,
        TimeProvider? timeProvider = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await EnsureTableAsync(connection, transaction, cancellationToken).ConfigureAwait(false);

            var options = _options.Value;
            var records = await LoadPendingAsync(connection, transaction, options, cancellationToken).ConfigureAwait(false);

            if (records.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success();
            }

            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var publishResult = await PublishAsync(record, cancellationToken).ConfigureAwait(false);
                if (publishResult.IsSuccess)
                {
                    await MarkAsDispatchedAsync(connection, transaction, record, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await MarkAsFailedAsync(connection, transaction, record, publishResult.Error.Message, cancellationToken).ConfigureAwait(false);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch outbox messages.");
            return Result.Failure(InfrastructureErrors.ExternalDependency("OUTBOX_DISPATCH_FAILED", ex.Message));
        }
    }

    private async Task<Result> PublishAsync(OutboxRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var messageType = ResolveMessageType(record.MessageType);
            if (messageType is null)
            {
                _logger.LogError("Cannot resolve message type {MessageType} from outbox record {Id}.", record.MessageType, record.Id);
                return Result.Failure(InfrastructureErrors.ValidationFailure($"Unknown message type {record.MessageType}"));
            }

            var message = (IMessage?)JsonSerializer.Deserialize(record.Payload, messageType, SerializerOptions);
            if (message is null)
            {
                return Result.Failure(InfrastructureErrors.ValidationFailure("Outbox payload deserialization returned null."));
            }

            var headers = record.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var envelope = new MessageEnvelope(message, record.Topic, record.TenantId, headers);
            return await _publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish outbox record {Id}.", record.Id);
            return Result.Failure(InfrastructureErrors.ExternalDependency("OUTBOX_PUBLISH_FAILED", ex.Message));
        }
    }

    private static Type? ResolveMessageType(string fullyQualifiedType)
    {
        var type = Type.GetType(fullyQualifiedType, throwOnError: false, ignoreCase: true);
        if (type is not null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(fullyQualifiedType, throwOnError: false, ignoreCase: true);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    private async Task EnsureTableAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"CREATE TABLE IF NOT EXISTS {options.TableName} (
    id uuid PRIMARY KEY,
    tenant_id text NULL,
    topic text NOT NULL,
    headers jsonb NOT NULL,
    payload jsonb NOT NULL,
    message_type text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    dispatched_at timestamptz NULL,
    attempts integer NOT NULL DEFAULT 0,
    last_error text NULL
);";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<OutboxRecord>> LoadPendingAsync(DbConnection connection, DbTransaction transaction, PostgresOutboxOptions options, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"SELECT id, tenant_id, topic, headers, payload, message_type, attempts
FROM {options.TableName}
WHERE dispatched_at IS NULL
  AND ( @maxAttempts <= 0 OR attempts < @maxAttempts )
ORDER BY created_at
FOR UPDATE SKIP LOCKED
LIMIT @batch;";
        AddParameter(command, "@batch", options.BatchSize);
        AddParameter(command, "@maxAttempts", options.MaxAttempts);

        var records = new List<OutboxRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetGuid(0);
            var tenantId = reader.IsDBNull(1) ? null : reader.GetString(1);
            var topic = reader.GetString(2);
            var headersJson = reader.GetString(3);
            var payloadJson = reader.GetString(4);
            var messageType = reader.GetString(5);
            var attempts = reader.GetInt32(6);

            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson, SerializerOptions) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            records.Add(new OutboxRecord(id, tenantId, topic, headers, payloadJson, messageType, attempts));
        }

        return records;
    }

    private async Task MarkAsDispatchedAsync(DbConnection connection, DbTransaction transaction, OutboxRecord record, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"UPDATE {_options.Value.TableName} SET dispatched_at = @dispatchedAt, attempts = attempts + 1, last_error = NULL WHERE id = @id";
        AddParameter(command, "@dispatchedAt", _timeProvider.GetUtcNow().UtcDateTime);
        AddParameter(command, "@id", record.Id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task MarkAsFailedAsync(DbConnection connection, DbTransaction transaction, OutboxRecord record, string failureReason, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"UPDATE {_options.Value.TableName} SET attempts = attempts + 1, last_error = @error WHERE id = @id";
        AddParameter(command, "@error", failureReason);
        AddParameter(command, "@id", record.Id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private sealed record OutboxRecord(
        Guid Id,
        string? TenantId,
        string Topic,
        Dictionary<string, string>? Headers,
        string Payload,
        string MessageType,
        int Attempts);
}



