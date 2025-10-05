using System.ComponentModel.DataAnnotations;

namespace YinaCRM.Infrastructure.Persistence;

public sealed class PostgresOptions
{
    [Required]
    public required string ConnectionString { get; init; }

    public int CommandTimeoutSeconds { get; init; } = 30;

    public int? MaxPoolSize { get; init; }

    public int? MinPoolSize { get; init; }
}
