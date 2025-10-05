using YinaCRM.Infrastructure.Abstractions.Persistence;

namespace YinaCRM.Infrastructure.Persistence;

public sealed class NoOpDatabaseMigrator : IDatabaseMigrator
{
    public Task ApplyMigrationsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
