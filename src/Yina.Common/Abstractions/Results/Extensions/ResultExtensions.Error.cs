#nullable enable
using System;
using System.Threading.Tasks;
using Yina.Common.Abstractions.Errors;

namespace Yina.Common.Abstractions.Results.Extensions;

public static partial class ResultExtensions
{
    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> mapper)
        => result.IsSuccess ? result : Result<T>.Failure(mapper(result.Error));

    public static Result MapError(this Result result, Func<Error, Error> mapper)
        => result.IsSuccess ? result : Result.Failure(mapper(result.Error));

    public static Result<T> Recover<T>(this Result<T> result, Func<Error, T> fallback)
        => result.IsSuccess ? result : Result<T>.Success(fallback(result.Error));

    public static Result<T> RecoverWith<T>(this Result<T> result, Func<Error, Result<T>> fallback)
        => result.IsSuccess ? result : fallback(result.Error);

    public static async Task<Result<T>> RecoverAsync<T>(this Result<T> result, Func<Error, Task<T>> fallback)
        => result.IsSuccess ? result : Result<T>.Success(await fallback(result.Error).ConfigureAwait(false));

    public static async Task<Result<T>> RecoverWithAsync<T>(this Result<T> result, Func<Error, Task<Result<T>>> fallback)
        => result.IsSuccess ? result : await fallback(result.Error).ConfigureAwait(false);

    public static async Task<Result<T>> MapErrorAsync<T>(this Task<Result<T>> resultTask, Func<Error, Error> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.MapError(mapper);
    }

    public static async Task<Result<T>> RecoverAsync<T>(this Task<Result<T>> resultTask, Func<Error, Task<T>> fallback)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.RecoverAsync(fallback).ConfigureAwait(false);
    }

    public static async Task<Result<T>> RecoverWithAsync<T>(this Task<Result<T>> resultTask, Func<Error, Task<Result<T>>> fallback)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.RecoverWithAsync(fallback).ConfigureAwait(false);
    }
}
