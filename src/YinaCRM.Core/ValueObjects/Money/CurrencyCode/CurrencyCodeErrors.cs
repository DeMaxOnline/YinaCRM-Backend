using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class CurrencyCodeErrors
{
    public static Error Empty() => Error.Create("CC_EMPTY", "Currency code is required", 400);
    public static Error Invalid() => Error.Create("CC_INVALID", "Currency code must be 3 letters (ISO-4217)", 400);
}



