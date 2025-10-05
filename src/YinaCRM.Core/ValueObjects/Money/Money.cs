// Placeholder VO: Money (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Monetary amount with currency.
/// Invariants: amount is non-negative by default; currency is required when amount is provided.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }
    internal CurrencyCode Currency { get; }

    private Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    public static Result<Money> TryCreate(decimal amount, string? currencyCode)
    {
        if (amount < 0)
            return Result<Money>.Failure(MoneyErrors.Negative());

        var cc = CurrencyCode.TryCreate(currencyCode);
        if (cc.IsFailure)
            return Result<Money>.Failure(MoneyErrors.CurrencyRequired());

        return Result<Money>.Success(new Money(amount, cc.Value));
    }

    public static Result<Money> TryCreate(decimal amount, CurrencyCode currency)
    {
        if (amount < 0)
            return Result<Money>.Failure(MoneyErrors.Negative());
        return Result<Money>.Success(new Money(amount, currency));
    }
}


