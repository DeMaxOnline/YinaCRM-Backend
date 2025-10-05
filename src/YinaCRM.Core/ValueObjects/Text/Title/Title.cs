// Placeholder VO: Title (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Title value object.
/// Normalization: trims and collapses whitespace to single space.
/// Validation: 1–200 chars.
/// </summary>
public readonly record struct Title
{
    internal string Value { get; }
    private Title(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Title> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Title>.Failure(TitleErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 200)
            return Result<Title>.Failure(TitleErrors.TooLong());
        return Result<Title>.Success(new Title(s));
    }
}


