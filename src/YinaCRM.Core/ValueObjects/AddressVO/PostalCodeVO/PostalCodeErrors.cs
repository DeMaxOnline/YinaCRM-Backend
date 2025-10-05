using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.PostalCodeVO;

public static class PostalCodeErrors
{
    public static Error Empty() => Error.Create("POSTAL_EMPTY", "Postal code is required", 400);
    public static Error Invalid() => Error.Create("POSTAL_INVALID", "Postal code format is invalid", 400);
}



