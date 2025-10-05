using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using Yina.Common.Protocols;
using YinaCRM.Core.ValueObjects;

namespace YinaCRM.Core.Entities.ClientEnvironment;

/// <summary>
/// Child value object for environment URLs. Each URL is absolute and typed by UrlTypeCode.
/// Immutable by design - create new instance for updates.
/// </summary>
public sealed class EnvUrl
{
    private EnvUrl(Guid id, UrlTypeCode typeCode, Url url, bool isPrimary)
    {
        Id = id;
        TypeCode = typeCode;
        Url = url;
        IsPrimary = isPrimary;
    }

    public Guid Id { get; }
    public UrlTypeCode TypeCode { get; }
    public Url Url { get; }
    public bool IsPrimary { get; }

    public static Result<EnvUrl> Create(UrlTypeCode typeCode, Url url, bool isPrimary = false)
        => Create(Guid.NewGuid(), typeCode, url, isPrimary);

    public static Result<EnvUrl> Create(Guid id, UrlTypeCode typeCode, Url url, bool isPrimary = false)
        => Result<EnvUrl>.Success(new EnvUrl(id, typeCode, url, isPrimary));

    /// <summary>
    /// Creates a new EnvUrl instance with updated values while preserving the ID.
    /// </summary>
    public Result<EnvUrl> WithUpdates(UrlTypeCode typeCode, Url url, bool isPrimary)
        => Create(Id, typeCode, url, isPrimary);
}

