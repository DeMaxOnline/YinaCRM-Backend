using System.Collections.Concurrent;
using System.Text;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Storage;

namespace YinaCRM.Infrastructure.Storage;

public sealed class InMemoryFileStorage : IFileStorage
{
    private sealed record StoredFile(
        byte[] Content,
        string ContentType,
        IReadOnlyDictionary<string, string> Metadata,
        DateTimeOffset StoredAtUtc,
        TimeSpan? TimeToLive);

    private readonly ConcurrentDictionary<string, StoredFile> _files = new();

    public Task<Result<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        request.Content.CopyTo(memory);
        var bytes = memory.ToArray();

        var key = BuildKey(request.TenantId, request.Path);
        var stored = new StoredFile(bytes, request.ContentType, request.Metadata, DateTimeOffset.UtcNow, request.TimeToLive);
        _files[key] = stored;

        var result = new FileUploadResult(
            new Uri($"https://local-storage/{request.TenantId}/{Uri.EscapeDataString(request.Path)}"),
            stored.StoredAtUtc,
            bytes.LongLength,
            stored.Metadata);

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<FileDownloadResult>> DownloadAsync(FileDownloadRequest request, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(request.TenantId, request.Path);
        if (!_files.TryGetValue(key, out var stored))
        {
            return Task.FromResult(Result.Failure<FileDownloadResult>(YinaCRM.Infrastructure.Support.InfrastructureErrors.ValidationFailure("File not found.")));
        }

        var stream = new MemoryStream(stored.Content, writable: false);
        Uri? signedUrl = null;
        DateTimeOffset? expiresAt = null;
        if (request.AsSignedUrl)
        {
            signedUrl = new Uri($"https://local-storage/{request.TenantId}/{Uri.EscapeDataString(request.Path)}?sig=fake");
            expiresAt = stored.StoredAtUtc + (request.ValidFor ?? TimeSpan.FromMinutes(5));
        }

        var result = new FileDownloadResult(
            stream,
            stored.ContentType,
            stored.Metadata,
            signedUrl,
            expiresAt);

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result> DeleteAsync(FileDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(request.TenantId, request.Path);
        _files.TryRemove(key, out _);
        return Task.FromResult(Result.Success());
    }

    private static string BuildKey(string tenantId, string path) => $"{tenantId}:{path}".ToLowerInvariant();
}
