using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class MoneyErrors
{
    public static Error Negative() => Error.Create("MONEY_NEGATIVE", "Amount cannot be negative", 400);
    public static Error CurrencyRequired() => Error.Create("MONEY_CURRENCY_REQUIRED", "Valid currency code is required", 400);
}



