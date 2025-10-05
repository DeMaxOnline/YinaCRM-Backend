using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Search;

public interface ISearchIndexer
{
    Task<Result> IndexAsync(SearchDocument document, CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(SearchDocumentReference reference, CancellationToken cancellationToken = default);
}

public sealed record SearchDocument(
    string Index,
    string Id,
    string TenantId,
    IReadOnlyDictionary<string, object> Fields,
    IReadOnlyDictionary<string, double>? Boosts,
    DateTimeOffset UpdatedAtUtc);

public sealed record SearchDocumentReference(
    string Index,
    string Id,
    string TenantId);
