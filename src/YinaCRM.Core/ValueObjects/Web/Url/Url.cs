// Placeholder VO: Url (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Absolute URL value object.
/// Normalization: trims; parses with <see cref="Uri"/> and stores normalized absolute URI string.
/// Validation: must be an absolute URI with scheme and host.
/// </summary>
public readonly record struct Url
{
    internal string Value { get; }
    private Url(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Url> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Url>.Failure(UrlErrors.Empty());
        var s = input.Trim();
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
            return Result<Url>.Failure(UrlErrors.Invalid());
        if (string.IsNullOrWhiteSpace(uri.Scheme) || string.IsNullOrWhiteSpace(uri.Host))
            return Result<Url>.Failure(UrlErrors.Invalid());

        var normalized = uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        return Result<Url>.Success(new Url(normalized));
    }
}


