namespace YinaCRM.Core.Abstractions;

public interface IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot
    where TId : struct
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
