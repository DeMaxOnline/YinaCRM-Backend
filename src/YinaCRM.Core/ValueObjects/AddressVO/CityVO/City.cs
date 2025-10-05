// Placeholder VO: City (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.AddressVO.CityVO;

/// <summary>
/// City name value object.
/// Normalization: trims and collapses whitespace; title-cases letters.
/// Validation: letters, spaces, hyphens and apostrophes; 1–100 chars.
/// </summary>
public readonly partial record struct City
{
    internal string Value { get; }
    private City(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<City> TryCreate(string? input)
    {
        var norm = Normalize(input);
        if (string.IsNullOrWhiteSpace(norm))
            return Result<City>.Failure(CityErrors.Empty());
        if (!CityPattern().IsMatch(norm))
            return Result<City>.Failure(CityErrors.Invalid());
        return Result<City>.Success(new City(norm));
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = Regex.Replace(input.Trim(), "\\s+", " ");
        try
        {
            var ti = CultureInfo.InvariantCulture.TextInfo;
            return ti.ToTitleCase(s.ToLowerInvariant());
        }
        catch
        {
            return s;
        }
    }

    [GeneratedRegex(@"^[\p{L} .'-]{1,100}$", RegexOptions.Compiled)]
    private static partial Regex CityPattern();
}


