using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.RegionVO.IsoCode;

public static class RegionIsoCodeErrors
{
    public static Error Empty() => Error.Create("REGIONISO_EMPTY", "Region ISO code is required", 400);
    public static Error Invalid() => Error.Create("REGIONISO_INVALID", "Region ISO code must match ^[A-Z]{2}-[A-Z0-9]{1,3}$", 400);
}



