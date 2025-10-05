// Placeholder VO: Description (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Description value object.
/// Normalization: trims and collapses whitespace.
/// Validation: 1–1000 chars.
/// </summary>
public readonly record struct Description
{
    internal string Value { get; }
    private Description(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Description> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Description>.Failure(DescriptionErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 1000)
            return Result<Description>.Failure(DescriptionErrors.TooLong());
        return Result<Description>.Success(new Description(s));
    }
}


