using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;

public static class AddressLineErrors
{
    public static Error Empty() => Error.Create("ADDRESS_EMPTY", "Address line is required", 400);
    public static Error Invalid() => Error.Create("ADDRESS_INVALID", "Address line has invalid characters or length", 400);
}



