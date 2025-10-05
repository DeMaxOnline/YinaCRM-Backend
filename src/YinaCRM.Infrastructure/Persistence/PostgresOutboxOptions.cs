using System;
using System.ComponentModel.DataAnnotations;

namespace YinaCRM.Infrastructure.Persistence;

public sealed class PostgresOutboxOptions
{
    [Required]
    public string TableName { get; init; } = "outbox_messages";

    public int BatchSize { get; init; } = 50;

    public int MaxAttempts { get; init; } = 10;

    public TimeSpan CleanupDispatchedAfter { get; init; } = TimeSpan.FromDays(7);

    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan LockTimeout { get; init; } = TimeSpan.FromSeconds(5);
}


