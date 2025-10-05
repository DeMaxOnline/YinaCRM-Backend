namespace YinaCRM.Infrastructure.Abstractions.Persistence;

public interface IDatabaseMigrator
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
}
