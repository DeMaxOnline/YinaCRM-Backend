#nullable enable
using System;
using Yina.Common.Abstractions.Errors;

namespace Yina.Common.Abstractions.Results.Extensions;

/// <summary>
/// Provides fluent combinators for composing <see cref="Result"/> and <see cref="Result{T}"/> flows.
/// </summary>
public static partial class ResultExtensions
{
    /// <summary>Chains another <see cref="Result"/>-producing operation when the source succeeds.</summary>
    public static Result Then(this Result result, Func<Result> binder)
        => result.IsSuccess ? binder() : result;

    /// <summary>Compensates a failed <see cref="Result"/> by executing <paramref name="compensation"/>.</summary>
    public static Result Compensate(this Result result, Func<Error, Result> compensation)
        => result.IsFailure ? compensation(result.Error) : result;

    /// <summary>Provides an alternative <see cref="Result"/> if the source fails.</summary>
    public static Result OrElse(this Result result, Func<Result> alternative)
        => result.IsSuccess ? result : alternative();

    public static Result<TResult> Select<TSource, TResult>(this Result<TSource> result, Func<TSource, TResult> selector)
        => result.Map(selector);

    public static Result<TResult> SelectMany<TSource, TResult>(this Result<TSource> result, Func<TSource, Result<TResult>> selector)
        => result.Bind(selector);

    public static Result<TResult> SelectMany<TSource, TIntermediate, TResult>(
        this Result<TSource> result,
        Func<TSource, Result<TIntermediate>> selector,
        Func<TSource, TIntermediate, TResult> projector)
        => result.Bind(src => selector(src).Map(mid => projector(src, mid)));

    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate)
    {
        if (!result.IsSuccess)
        {
            return result;
        }

        return result.TryGetValue(out var value) && predicate(value)
            ? result
            : Result<T>.Failure(Error.Create("PREDICATE_FAILED", "Predicate failed for value", 400));
    }

    public static Result<T> Flatten<T>(this Result<Result<T>> result)
        => result.IsSuccess ? result.Value : Result<T>.Failure(result.Error);

    public static Result<T> Compensate<T>(this Result<T> result, Func<Error, Result<T>> compensation)
        => result.IsFailure ? compensation(result.Error) : result;

    public static Result<T> OrElse<T>(this Result<T> result, Func<Result<T>> alternative)
        => result.IsSuccess ? result : alternative();
}
