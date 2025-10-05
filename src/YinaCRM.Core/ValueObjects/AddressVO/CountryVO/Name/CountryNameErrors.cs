using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Name;

public static class CountryNameErrors
{
    public static Error Empty() => Error.Create("COUNTRY_EMPTY", "Country name is required", 400);
    public static Error Invalid() => Error.Create("COUNTRY_INVALID", "Country name has invalid characters or length", 400);
}



