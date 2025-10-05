namespace Yina.Common.Abstractions.Results;

using System.Diagnostics.CodeAnalysis;
using Yina.Common.Abstractions.Errors;

public readonly struct Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error)
    {
        if (error.IsNone)
        {
            throw new ArgumentException("Use Error.None only for successful results.", nameof(error));
        }

        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public Result Ensure(Func<bool> predicate, Error error) => IsSuccess && !predicate() ? Failure(error) : this;

    public Result<T> To<T>(T value) => IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error);

    public Result OnSuccess(Func<Result> next) => IsSuccess ? next() : this;

    public Result OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            action(Error);
        }

        return this;
    }

    public static Result Try(Action action, Func<Exception, Error> errorFactory)
    {
        try
        {
            action();
            return Success();
        }
        catch (Exception ex)
        {
            return Failure(errorFactory(ex));
        }
    }

    public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
}

public readonly struct Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Value is unavailable for failed results.");

    public T? ValueOrDefault(T? fallback = default) => IsSuccess ? _value : fallback;

    public static Result<T> Success(T value) => new(true, value, Error.None);

    public static Result<T> Failure(Error error)
    {
        if (error.IsNone)
        {
            throw new ArgumentException("Use Error.None only for successful results.", nameof(error));
        }

        return new Result<T>(false, default, error);
    }

    public Result<K> Map<K>(Func<T, K> mapper) => IsSuccess ? Result<K>.Success(mapper(Value)) : Result<K>.Failure(Error);

    public Result<K> Bind<K>(Func<T, Result<K>> binder) => IsSuccess ? binder(Value) : Result<K>.Failure(Error);

    public Result<T> Ensure(Func<T, bool> predicate, Error error)
    {
        if (IsFailure || predicate(Value))
        {
            return this;
        }

        return Failure(error);
    }

    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            action(Error);
        }

        return this;
    }

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (IsSuccess)
        {
            value = Value;
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";

    public static implicit operator Result<T>(T value) => Success(value);
}
