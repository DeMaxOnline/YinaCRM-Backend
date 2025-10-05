// Placeholder VO: CurrencyCode (shared)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// ISO-4217 currency code.
/// Normalization: uppercases and trims.
/// Validation: exactly 3 letters A–Z.
/// </summary>
public readonly partial record struct CurrencyCode
{
    internal string Value { get; }
    private CurrencyCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<CurrencyCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<CurrencyCode>.Failure(CurrencyCodeErrors.Empty());
        var s = input.Trim().ToUpperInvariant();
        if (!CurrencyPattern().IsMatch(s))
            return Result<CurrencyCode>.Failure(CurrencyCodeErrors.Invalid());
        return Result<CurrencyCode>.Success(new CurrencyCode(s));
    }

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.Compiled)]
    private static partial Regex CurrencyPattern();
}


