// Placeholder VO: PostalCode (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;

/// <summary>
/// Postal code value object.
/// Normalization: trims and uppercases; collapses inner whitespace.
/// Validation: 2–12 alphanumeric with optional spaces or hyphens.
/// </summary>
public readonly partial record struct PostalCode
{
    internal string Value { get; }
    private PostalCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<PostalCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<PostalCode>.Failure(PostalCodeErrors.Empty());
        var s = Regex.Replace(input.Trim().ToUpperInvariant(), "\\s+", " ");
        if (!PostalPattern().IsMatch(s))
            return Result<PostalCode>.Failure(PostalCodeErrors.Invalid());
        return Result<PostalCode>.Success(new PostalCode(s));
    }

    [GeneratedRegex("^[A-Z0-9 -]{2,12}$", RegexOptions.Compiled)]
    private static partial Regex PostalPattern();
}


