using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Storage;

public interface IFileStorage
{
    Task<Result<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    Task<Result<FileDownloadResult>> DownloadAsync(FileDownloadRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(FileDeleteRequest request, CancellationToken cancellationToken = default);
}

public sealed record FileUploadRequest(
    string TenantId,
    string Path,
    string ContentType,
    Stream Content,
    IReadOnlyDictionary<string, string> Metadata,
    TimeSpan? TimeToLive);

public sealed record FileUploadResult(
    Uri Uri,
    DateTimeOffset UploadedAtUtc,
    long SizeBytes,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record FileDownloadRequest(
    string TenantId,
    string Path,
    bool AsSignedUrl,
    TimeSpan? ValidFor);

public sealed record FileDownloadResult(
    Stream Content,
    string ContentType,
    IReadOnlyDictionary<string, string> Metadata,
    Uri? SignedUrl,
    DateTimeOffset? ExpiresAtUtc);

public sealed record FileDeleteRequest(
    string TenantId,
    string Path,
    bool HardDelete);
