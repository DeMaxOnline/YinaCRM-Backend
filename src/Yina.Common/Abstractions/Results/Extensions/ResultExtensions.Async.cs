#nullable enable
using System;
using System.Threading.Tasks;
using Yina.Common.Abstractions.Errors;

namespace Yina.Common.Abstractions.Results.Extensions;

public static partial class ResultExtensions
{
    public static async Task<Result<T>> EnsureAsync<T>(this Result<T> result, Func<T, Task<bool>> predicate, Error error)
    {
        if (!result.IsSuccess || !result.TryGetValue(out var value))
        {
            return result;
        }

        var ok = await predicate(value).ConfigureAwait(false);
        return ok ? result : Result<T>.Failure(error);
    }

    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.TryGetValue(out var value))
        {
            await action(value).ConfigureAwait(false);
        }

        return result;
    }

    public static Task<TResult> MatchAsync<T, TResult>(this Result<T> result, Func<T, Task<TResult>> onSuccess, Func<Error, Task<TResult>> onFailure)
        => result.TryGetValue(out var value) ? onSuccess(value) : onFailure(result.Error);

    public static async Task MatchAsync<T>(this Result<T> result, Func<T, Task> onSuccess, Func<Error, Task> onFailure)
    {
        if (result.TryGetValue(out var value))
        {
            await onSuccess(value).ConfigureAwait(false);
        }
        else
        {
            await onFailure(result.Error).ConfigureAwait(false);
        }
    }

    public static async Task MatchAsync(this Result result, Func<Task> onSuccess, Func<Error, Task> onFailure)
    {
        if (result.IsSuccess)
        {
            await onSuccess().ConfigureAwait(false);
        }
        else
        {
            await onFailure(result.Error).ConfigureAwait(false);
        }
    }
}
