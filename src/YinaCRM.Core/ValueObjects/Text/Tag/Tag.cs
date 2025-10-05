// Placeholder VO: Tag (shared)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Tag value object for labels.
/// Normalization: lowercases and trims.
/// Validation: 1–50 chars, pattern: ^[a-z0-9][a-z0-9-]{0,49}$.
/// </summary>
public readonly partial record struct Tag
{
    internal string Value { get; }
    private Tag(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Tag> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Tag>.Failure(TagErrors.Empty());
        var s = input.Trim().ToLowerInvariant();
        if (!TagPattern().IsMatch(s))
            return Result<Tag>.Failure(TagErrors.Invalid());
        return Result<Tag>.Success(new Tag(s));
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,49}$", RegexOptions.Compiled)]
    private static partial Regex TagPattern();
}


