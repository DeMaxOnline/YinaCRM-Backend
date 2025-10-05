using System.Data.Common;

namespace YinaCRM.Infrastructure.Abstractions.Persistence;

public interface IDatabaseConnectionFactory
{
    ValueTask<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
