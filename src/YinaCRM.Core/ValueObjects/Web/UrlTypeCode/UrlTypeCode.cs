// Placeholder VO: UrlTypeCode (shared)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// URL type code value object (e.g., "yinaapiurl", "webshop-b2c").
/// Normalization: lowercases and trims.
/// Validation: 1–64 chars, pattern ^[a-z0-9][a-z0-9-]{0,63}$. Examples documented; not restricted to a fixed set.
/// </summary>
public readonly partial record struct UrlTypeCode
{
    internal string Value { get; }
    private UrlTypeCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<UrlTypeCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<UrlTypeCode>.Failure(UrlTypeCodeErrors.Empty());
        var s = input.Trim().ToLowerInvariant();
        if (!UrlTypePattern().IsMatch(s))
            return Result<UrlTypeCode>.Failure(UrlTypeCodeErrors.Invalid());
        return Result<UrlTypeCode>.Success(new UrlTypeCode(s));
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.Compiled)]
    private static partial Regex UrlTypePattern();
}


