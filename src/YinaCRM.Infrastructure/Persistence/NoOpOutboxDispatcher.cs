using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Persistence;

namespace YinaCRM.Infrastructure.Persistence;

public sealed class NoOpOutboxDispatcher : IOutboxDispatcher
{
    public Task<Result> DispatchPendingAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
