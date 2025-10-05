// Placeholder VO: CountryCode (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Code;

/// <summary>
/// Country code value object (ISO 3166-1 alpha-2 or alpha-3).
/// Normalization: uppercases and trims.
/// Validation: exactly 2 or 3 letters A–Z.
/// </summary>
public readonly partial record struct CountryCode
{
    internal string Value { get; }
    private CountryCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<CountryCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<CountryCode>.Failure(CountryCodeErrors.Empty());
        var s = input.Trim().ToUpperInvariant();
        if (!CountryCodePattern().IsMatch(s))
            return Result<CountryCode>.Failure(CountryCodeErrors.Invalid());
        return Result<CountryCode>.Success(new CountryCode(s));
    }

    [GeneratedRegex("^[A-Z]{2,3}$", RegexOptions.Compiled)]
    private static partial Regex CountryCodePattern();
}


