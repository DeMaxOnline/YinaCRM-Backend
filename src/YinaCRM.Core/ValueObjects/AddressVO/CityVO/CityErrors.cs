using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.CityVO;

public static class CityErrors
{
    public static Error Empty() => Error.Create("CITY_EMPTY", "City is required", 400);
    public static Error Invalid() => Error.Create("CITY_INVALID", "City has invalid characters or length", 400);
}



