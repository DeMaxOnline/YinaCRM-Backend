namespace Yina.Common.Foundation;

using System.Diagnostics.CodeAnalysis;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

public readonly struct Maybe<T>
{
    private readonly T? _value;

    private Maybe(T? value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    public bool HasValue { get; }

    public bool IsNone => !HasValue;

    public T Value => HasValue ? _value! : throw new InvalidOperationException("Maybe has no value.");

    public T ValueOrDefault(T fallback) => HasValue ? _value! : fallback;

    public static Maybe<T> None() => new(default, false);

    public static Maybe<T> From(T? value) => value is null ? None() : new Maybe<T>(value, true);

    public Maybe<K> Map<K>(Func<T, K> mapper) => HasValue ? Maybe<K>.From(mapper(Value)) : Maybe<K>.None();

    public Result<T> ToResult(Error error) => HasValue ? Result<T>.Success(Value) : Result<T>.Failure(error);

    public bool TryGetValue([MaybeNullWhen(false)] out T result)
    {
        if (HasValue)
        {
            result = _value!;
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => HasValue ? $"Some: {Value}" : "None";
}
