using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Search;

namespace YinaCRM.Infrastructure.Search;

public sealed class NoOpSearchIndexer : ISearchIndexer
{
    public Task<Result> IndexAsync(SearchDocument document, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> RemoveAsync(SearchDocumentReference reference, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
