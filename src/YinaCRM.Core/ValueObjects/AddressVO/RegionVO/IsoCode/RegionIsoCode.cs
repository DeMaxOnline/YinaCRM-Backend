// Placeholder VO: RegionIsoCode (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.AddressVO.RegionVO.IsoCode;

/// <summary>
/// Region ISO code value object (ISO 3166-2 style, e.g., US-CA).
/// Normalization: uppercases and trims.
/// Validation: pattern ^[A-Z]{2}-[A-Z0-9]{1,3}$.
/// </summary>
public readonly partial record struct RegionIsoCode
{
    internal string Value { get; }
    private RegionIsoCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<RegionIsoCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<RegionIsoCode>.Failure(RegionIsoCodeErrors.Empty());
        var s = input.Trim().ToUpperInvariant();
        if (!RegionIsoPattern().IsMatch(s))
            return Result<RegionIsoCode>.Failure(RegionIsoCodeErrors.Invalid());
        return Result<RegionIsoCode>.Success(new RegionIsoCode(s));
    }

    [GeneratedRegex("^[A-Z]{2}-[A-Z0-9]{1,3}$", RegexOptions.Compiled)]
    private static partial Regex RegionIsoPattern();
}


