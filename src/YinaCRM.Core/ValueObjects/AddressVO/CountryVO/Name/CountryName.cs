// Placeholder VO: CountryName (shared)
#nullable enable
using System.Text.RegularExpressions;
using System.Globalization;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name;

/// <summary>
/// Country name value object.
/// Normalization: trims, collapses spaces, title-cases letters.
/// Validation: letters, spaces and hyphens; 2–56 chars.
/// </summary>
public readonly partial record struct CountryName
{
    internal string Value { get; }
    private CountryName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<CountryName> TryCreate(string? input)
    {
        var norm = Normalize(input);
        if (string.IsNullOrWhiteSpace(norm))
            return Result<CountryName>.Failure(CountryNameErrors.Empty());
        if (!CountryPattern().IsMatch(norm))
            return Result<CountryName>.Failure(CountryNameErrors.Invalid());
        return Result<CountryName>.Success(new CountryName(norm));
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = Regex.Replace(input.Trim(), "\\s+", " ");
        var ti = CultureInfo.InvariantCulture.TextInfo;
        return ti.ToTitleCase(s.ToLowerInvariant());
    }

    [GeneratedRegex(@"^[\p{L} -]{2,56}$", RegexOptions.Compiled)]
    private static partial Regex CountryPattern();
}


