// VO: CompanyName (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// CompanyName value object.
/// Normalization: trims and collapses whitespace.
/// Validation: 1–200 characters.
/// </summary>
public readonly record struct CompanyName
{
    internal string Value { get; }
    private CompanyName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<CompanyName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<CompanyName>.Failure(CompanyNameErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 200)
            return Result<CompanyName>.Failure(CompanyNameErrors.TooLong());
        return Result<CompanyName>.Success(new CompanyName(s));
    }
}


