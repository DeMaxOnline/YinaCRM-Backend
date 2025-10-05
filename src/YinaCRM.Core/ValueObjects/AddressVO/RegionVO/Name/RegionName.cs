// Placeholder VO: RegionName (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.AddressVO.RegionVO.Name;

/// <summary>
/// Region name value object (e.g., State/Province name).
/// Normalization: trims and collapses whitespace; title-cases letters.
/// Validation: letters, spaces, hyphens and apostrophes; 1–100 chars.
/// </summary>
public readonly partial record struct RegionName
{
    internal string Value { get; }
    private RegionName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<RegionName> TryCreate(string? input)
    {
        var s = input is null ? string.Empty : Regex.Replace(input.Trim(), "\\s+", " ");
        if (string.IsNullOrWhiteSpace(s))
            return Result<RegionName>.Failure(RegionNameErrors.Empty());
        if (!RegionNamePattern().IsMatch(s))
            return Result<RegionName>.Failure(RegionNameErrors.Invalid());
        return Result<RegionName>.Success(new RegionName(System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant())));
    }

    [GeneratedRegex(@"^[\p{L} .'-]{1,100}$", RegexOptions.Compiled)]
    private static partial Regex RegionNamePattern();
}


