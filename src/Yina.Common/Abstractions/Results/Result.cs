namespace Yina.Common.Abstractions.Results;

using System;
using System.Diagnostics.CodeAnalysis;
using Yina.Common.Abstractions.Errors;

/// <summary>
/// Represents the outcome of an operation without a value payload.
/// </summary>
public readonly struct Result
{
    private const byte StateDefault = 0;
    private const byte StateSuccess = 1;
    private const byte StateFailure = 2;

    private readonly byte _state;
    private readonly Error? _error;

    private Result(byte state, Error error)
    {
        _state = state;
        _error = error;
    }

    /// <summary>Gets a value indicating whether the result represents success.</summary>
    public bool IsSuccess => _state != StateFailure;

    /// <summary>Gets a value indicating whether the result represents failure.</summary>
    public bool IsFailure => _state == StateFailure;

    /// <summary>Gets the associated error when the result is a failure; returns <see cref="Error.None"/> otherwise.</summary>
    public Error Error => _state == StateFailure ? _error ?? Error.None : Error.None;

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(StateSuccess, Error.None);

    /// <summary>Creates a failed result with the supplied <paramref name="error"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="error"/> is <see cref="Error.None"/>.</exception>
    public static Result Failure(Error error)
    {
        if (error.IsNone)
        {
            throw new ArgumentException("Use Error.None only for successful results.", nameof(error));
        }

        return new Result(StateFailure, error);
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public Result Ensure(Func<bool> predicate, Error error)
        => IsSuccess && !predicate() ? Failure(error) : this;

    public Result<T> To<T>(T value)
        => IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error);

    /// <summary>Executes the provided <paramref name="action"/> and returns a <see cref="Result"/> capturing exceptions.</summary>
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

/// <summary>
/// Represents the outcome of an operation with a typed value payload.
/// </summary>
public readonly struct Result<T>
{
    private const byte StateDefault = 0;
    private const byte StateSuccess = 1;
    private const byte StateFailure = 2;

    private readonly byte _state;
    private readonly T? _value;
    private readonly Error? _error;

    private Result(byte state, T? value, Error error)
    {
        _state = state;
        _value = value;
        _error = error;
    }

    /// <summary>Gets a value indicating whether the result represents success.</summary>
    public bool IsSuccess => _state != StateFailure;

    /// <summary>Gets a value indicating whether the result represents failure.</summary>
    public bool IsFailure => _state == StateFailure;

    /// <summary>Gets the associated error when the result is a failure; returns <see cref="Error.None"/> otherwise.</summary>
    public Error Error => _state == StateFailure ? _error ?? Error.None : Error.None;

    /// <summary>Gets the value produced by a successful result.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing the value of a failed result.</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Value is unavailable for failed results.");

    /// <summary>Gets the value if successful or a fallback when failed.</summary>
    public T? ValueOrDefault(T? fallback = default) => IsSuccess ? _value : fallback;

    /// <summary>Creates a successful result containing <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(StateSuccess, value, Error.None);

    /// <summary>Creates a failed result with the supplied <paramref name="error"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="error"/> is <see cref="Error.None"/>.</exception>
    public static Result<T> Failure(Error error)
    {
        if (error.IsNone)
        {
            throw new ArgumentException("Use Error.None only for successful results.", nameof(error));
        }

        return new Result<T>(StateFailure, default, error);
    }

    public Result<K> Map<K>(Func<T, K> mapper)
        => IsSuccess ? Result<K>.Success(mapper(Value)) : Result<K>.Failure(Error);

    public Result<K> Bind<K>(Func<T, Result<K>> binder)
        => IsSuccess ? binder(Value) : Result<K>.Failure(Error);

    public Result<T> Ensure(Func<T, bool> predicate, Error error)
    {
        if (IsFailure || predicate(Value))
        {
            return this;
        }

        return Failure(error);
    }

    /// <summary>Attempts to retrieve the value without throwing.</summary>
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
