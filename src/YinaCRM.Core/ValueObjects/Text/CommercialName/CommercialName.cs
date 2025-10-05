// VO: CommercialName (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// CommercialName value object.
/// Normalization: trims and collapses whitespace.
/// Validation: 1–200 characters.
/// </summary>
public readonly record struct CommercialName
{
    internal string Value { get; }
    private CommercialName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<CommercialName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<CommercialName>.Failure(CommercialNameErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 200)
            return Result<CommercialName>.Failure(CommercialNameErrors.TooLong());
        return Result<CommercialName>.Success(new CommercialName(s));
    }
}


