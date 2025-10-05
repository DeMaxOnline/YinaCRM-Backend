using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.CountryVO.Code;

public static class CountryCodeErrors
{
    public static Error Empty() => Error.Create("COUNTRYCODE_EMPTY", "Country code is required", 400);
    public static Error Invalid() => Error.Create("COUNTRYCODE_INVALID", "Country code must be 2 or 3 letters (ISO 3166-1)", 400);
}



