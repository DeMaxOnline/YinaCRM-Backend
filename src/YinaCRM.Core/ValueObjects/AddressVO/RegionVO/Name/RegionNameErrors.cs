using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.RegionVO.Name;

public static class RegionNameErrors
{
    public static Error Empty() => Error.Create("REGIONNAME_EMPTY", "Region name is required", 400);
    public static Error Invalid() => Error.Create("REGIONNAME_INVALID", "Region name has invalid characters or length", 400);
}



