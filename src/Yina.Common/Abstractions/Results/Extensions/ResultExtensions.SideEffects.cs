#nullable enable
using System;
using System.Threading.Tasks;
using Yina.Common.Abstractions.Errors;

namespace Yina.Common.Abstractions.Results.Extensions;

/// <summary>Side-effect hooks that leave the source result unchanged.</summary>
public static partial class ResultExtensions
{
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.TryGetValue(out var value))
        {
            action(value);
        }

        return result;
    }

    public static Result<T> OnFailure<T>(this Result<T> result, Action<Error> action)
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    public static async Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.TryGetValue(out var value))
        {
            await action(value).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result<T>> OnFailureAsync<T>(this Task<Result<T>> resultTask, Func<Error, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            await action(result.Error).ConfigureAwait(false);
        }

        return result;
    }

    public static Result<T> TapError<T>(this Result<T> result, Action<Error> errorAction)
    {
        if (result.IsFailure)
        {
            errorAction(result.Error);
        }

        return result;
    }

    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static Result OnFailure(this Result result, Action<Error> action)
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    public static async Task<Result> OnSuccessAsync(this Task<Result> resultTask, Func<Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result> OnFailureAsync(this Task<Result> resultTask, Func<Error, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            await action(result.Error).ConfigureAwait(false);
        }

        return result;
    }

    public static Result TapError(this Result result, Action<Error> errorAction)
    {
        if (result.IsFailure)
        {
            errorAction(result.Error);
        }

        return result;
    }
}


