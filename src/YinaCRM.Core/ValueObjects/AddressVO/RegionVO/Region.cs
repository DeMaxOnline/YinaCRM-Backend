// Placeholder VO: Region (composite)
#nullable enable
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.ValueObjects.AddressVO.RegionVO.IsoCode;
using YinaCRM.Core.ValueObjects.AddressVO.RegionVO.Name;

namespace YinaCRM.Core.ValueObjects.AddressVO.RegionVO;

/// <summary>
/// Region value object composed of <see cref="RegionName"/> and <see cref="RegionIsoCode"/>.
/// </summary>
public readonly record struct Region
{
    public RegionName Name { get; }
    public RegionIsoCode IsoCode { get; }

    private Region(RegionName name, RegionIsoCode iso)
    {
        Name = name;
        IsoCode = iso;
    }

    public override string ToString() => $"{Name} ({IsoCode})";

    public static Result<Region> TryCreate(string? name, string? isoCode)
    {
        var n = RegionName.TryCreate(name);
        if (n.IsFailure) return Result<Region>.Failure(n.Error);

        var iso = RegionIsoCode.TryCreate(isoCode);
        if (iso.IsFailure) return Result<Region>.Failure(iso.Error);

        return Result<Region>.Success(new Region(n.Value, iso.Value));
    }
}


