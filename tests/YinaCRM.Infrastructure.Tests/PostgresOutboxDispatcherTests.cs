using System;\r\nusing System.Collections.Generic;\r\nusing System.Threading;\r\nusing System.Threading.Tasks;\r\nusing System.Collections.Immutable;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Xunit;
using Yina.Common.Abstractions.Results;
using Yina.Common.Protocols;
using Yina.Common.Serialization;
using YinaCRM.Infrastructure.Abstractions.Messaging;
using YinaCRM.Infrastructure.Persistence;

namespace YinaCRM.Infrastructure.Tests;

public sealed class PostgresOutboxDispatcherTests : IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer _container = new TestcontainersBuilder<PostgreSqlTestcontainer>()
        .WithDatabase(new PostgreSqlTestcontainerConfiguration())
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync();

    [Fact]
    public async Task DispatchPendingAsync_PublishesAndMarksDispatched()
    {
        var postgresOptions = new PostgresOptions
        {
            ConnectionString = _container.ConnectionString
        };

        var outboxOptions = new PostgresOutboxOptions
        {
            TableName = "outbox_messages"
        };

        var connectionFactory = new PostgresConnectionFactory(
            new StubOptionsMonitor<PostgresOptions>(postgresOptions),
            NullLogger<PostgresConnectionFactory>.Instance);

        var publisher = new FakePublisher();
        var dispatcher = new PostgresOutboxDispatcher(
            connectionFactory,
            publisher,
            new OptionsWrapper<PostgresOutboxOptions>(outboxOptions),
            NullLogger<PostgresOutboxDispatcher>.Instance,
            TimeProvider.System);

        // Ensure table exists
        await dispatcher.DispatchPendingAsync();

        var messageId = Guid.NewGuid();
        var headers = ImmutableDictionary<string, string>.Empty.Add("correlation_id", "abc-123");
        var payload = JsonSerializer.Serialize(new IntegrationTestMessage("outbox"), JsonDefaults.Create());
        var headersJson = JsonSerializer.Serialize(headers, JsonDefaults.Create());
        var messageType = typeof(IntegrationTestMessage).AssemblyQualifiedName!;

        await using (var connection = new NpgsqlConnection(_container.ConnectionString))
        {
            await connection.OpenAsync();
            await using var insert = new NpgsqlCommand($"INSERT INTO {outboxOptions.TableName} (id, tenant_id, topic, headers, payload, message_type, created_at) VALUES (@id, @tenant, @topic, @headers, @payload, @type, now())", connection);
            insert.Parameters.AddWithValue("id", messageId);
            insert.Parameters.AddWithValue("tenant", "tenant-outbox");
            insert.Parameters.AddWithValue("topic", "crm.events.test");
            insert.Parameters.AddWithValue("headers", NpgsqlDbType.Jsonb, headersJson);
            insert.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payload);
            insert.Parameters.AddWithValue("type", messageType);
            await insert.ExecuteNonQueryAsync();
        }

        var result = await dispatcher.DispatchPendingAsync();
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Single(publisher.PublishedMessages);

        await using (var connection = new NpgsqlConnection(_container.ConnectionString))
        {
            await connection.OpenAsync();
            await using var select = new NpgsqlCommand($"SELECT dispatched_at, attempts, last_error FROM {outboxOptions.TableName} WHERE id = @id", connection);
            select.Parameters.AddWithValue("id", messageId);
            await using var reader = await select.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.False(reader.IsDBNull(0));
            Assert.Equal(1, reader.GetInt32(1));
            Assert.True(reader.IsDBNull(2));
        }
    }

    [Fact]
    public async Task DispatchPendingAsync_RecordsFailureWhenPublishFails()
    {
        var postgresOptions = new PostgresOptions { ConnectionString = _container.ConnectionString };
        var outboxOptions = new PostgresOutboxOptions { TableName = "outbox_messages" };

        var connectionFactory = new PostgresConnectionFactory(
            new StubOptionsMonitor<PostgresOptions>(postgresOptions),
            NullLogger<PostgresConnectionFactory>.Instance);

        var publisher = new FakePublisher
        {
            PublishOverride = _ => Task.FromResult(Result.Failure(InfrastructureErrors.ExternalDependency("PUBLISH_FAIL", "boom")))
        };

        var dispatcher = new PostgresOutboxDispatcher(
            connectionFactory,
            publisher,
            new OptionsWrapper<PostgresOutboxOptions>(outboxOptions),
            NullLogger<PostgresOutboxDispatcher>.Instance,
            TimeProvider.System);

        await dispatcher.DispatchPendingAsync();

        var messageId = Guid.NewGuid();
        var headersJson = JsonSerializer.Serialize(ImmutableDictionary<string, string>.Empty, JsonDefaults.Create());
        var payloadJson = JsonSerializer.Serialize(new IntegrationTestMessage("fail"), JsonDefaults.Create());
        var messageType = typeof(IntegrationTestMessage).AssemblyQualifiedName!;

        await using (var connection = new NpgsqlConnection(_container.ConnectionString))
        {
            await connection.OpenAsync();
            await using var insert = new NpgsqlCommand($"INSERT INTO {outboxOptions.TableName} (id, tenant_id, topic, headers, payload, message_type, created_at) VALUES (@id, @tenant, @topic, @headers, @payload, @type, now())", connection);
            insert.Parameters.AddWithValue("id", messageId);
            insert.Parameters.AddWithValue("tenant", DBNull.Value);
            insert.Parameters.AddWithValue("topic", "crm.events.fail");
            insert.Parameters.AddWithValue("headers", NpgsqlDbType.Jsonb, headersJson);
            insert.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payloadJson);
            insert.Parameters.AddWithValue("type", messageType);
            await insert.ExecuteNonQueryAsync();
        }

        var result = await dispatcher.DispatchPendingAsync();
        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Empty(publisher.PublishedMessages);

        await using (var connection = new NpgsqlConnection(_container.ConnectionString))
        {
            await connection.OpenAsync();
            await using var select = new NpgsqlCommand($"SELECT dispatched_at, attempts, last_error FROM {outboxOptions.TableName} WHERE id = @id", connection);
            select.Parameters.AddWithValue("id", messageId);
            await using var reader = await select.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.True(reader.IsDBNull(0));
            Assert.Equal(1, reader.GetInt32(1));
            Assert.False(reader.IsDBNull(2));
            Assert.Contains("boom", reader.GetString(2));
        }
    }

    private sealed class FakePublisher : IMessagePublisher
    {
        public List<MessageEnvelope> PublishedMessages { get; } = new();

        public Func<MessageEnvelope, Task<Result>>? PublishOverride { get; set; }

        public Task<Result> PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (PublishOverride is not null)
            {
                return PublishOverride(envelope);
            }

            PublishedMessages.Add(envelope);
            return Task.FromResult(Result.Success());
        }
    }

    private sealed record IntegrationTestMessage(string Name) : IMessage;
}

