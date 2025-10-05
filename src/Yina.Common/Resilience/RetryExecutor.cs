using System;
using System.Threading;
using System.Threading.Tasks;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace Yina.Common.Resilience;

/// <summary>Executes operations with retry/backoff and optional per-attempt timeout.</summary>
public static class RetryExecutor
{
    /// <summary>
    /// Executes the provided <paramref name="action"/> with retry semantics.
    /// </summary>
    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        RetryOptions options,
        IRetryClassifier? classifier = null,
        IBackoffStrategy? backoff = null,
        Action<int, Exception>? onRetry = null,
        CancellationToken ct = default)
    {
        classifier ??= new DefaultRetryClassifier();
        backoff ??= new JitteredExponentialBackoffStrategy(options.BaseDelay, options.MaxDelay);
        var attempts = 0;
        Exception? lastEx = null;

        while (attempts < options.MaxAttempts && !ct.IsCancellationRequested)
        {
            attempts++;
            var attemptTimeout = options.AttemptTimeout;
            using var linkedCts = attemptTimeout is TimeSpan
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : null;

            if (linkedCts is not null && attemptTimeout is TimeSpan timeout)
            {
                linkedCts.CancelAfter(timeout);
            }

            var actCt = linkedCts?.Token ?? ct;
            try
            {
                await action(actCt).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (classifier.IsTransient(ex) && attempts < options.MaxAttempts && !ct.IsCancellationRequested)
            {
                lastEx = ex;
                onRetry?.Invoke(attempts, ex);
                await Task.Delay(backoff.GetDelay(attempts), ct).ConfigureAwait(false);
            }
        }

        throw lastEx ?? new OperationCanceledException();
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> returning a value with retry semantics.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        RetryOptions options,
        IRetryClassifier? classifier = null,
        IBackoffStrategy? backoff = null,
        Action<int, Exception>? onRetry = null,
        CancellationToken ct = default)
    {
        T? result = default;
        await ExecuteAsync(async token => { result = await action(token).ConfigureAwait(false); }, options, classifier, backoff, onRetry, ct)
            .ConfigureAwait(false);

        if (result is null)
        {
            throw new InvalidOperationException("Action returned null result.");
        }

        return result;
    }

    /// <summary>
    /// Executes the provided <paramref name="action"/> returning <see cref="Result{T}"/> with retry semantics.
    /// </summary>
    public static async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> action,
        RetryOptions options,
        IRetryClassifier? classifier = null,
        IBackoffStrategy? backoff = null,
        Action<int, Exception>? onRetry = null,
        CancellationToken ct = default)
    {
        classifier ??= new DefaultRetryClassifier();
        backoff ??= new JitteredExponentialBackoffStrategy(options.BaseDelay, options.MaxDelay);
        var attempts = 0;
        Exception? lastEx = null;
        Result<T> lastResult = default;
        var hadResult = false;

        while (attempts < options.MaxAttempts && !ct.IsCancellationRequested)
        {
            attempts++;
            var attemptTimeout = options.AttemptTimeout;
            using var linkedCts = attemptTimeout is TimeSpan
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : null;

            if (linkedCts is not null && attemptTimeout is TimeSpan timeout)
            {
                linkedCts.CancelAfter(timeout);
            }

            var actCt = linkedCts?.Token ?? ct;

            try
            {
                var result = await action(actCt).ConfigureAwait(false);
                if (!result.IsFailure || !result.Error.IsRetryable() || attempts >= options.MaxAttempts)
                {
                    return result;
                }

                lastResult = result;
                hadResult = true;
                await Task.Delay(backoff.GetDelay(attempts), ct).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) when (classifier.IsTransient(ex) && attempts < options.MaxAttempts && !ct.IsCancellationRequested)
            {
                lastEx = ex;
                onRetry?.Invoke(attempts, ex);
                await Task.Delay(backoff.GetDelay(attempts), ct).ConfigureAwait(false);
            }
        }

        if (lastEx is not null)
        {
            throw lastEx;
        }

        if (hadResult)
        {
            return lastResult;
        }

        return Result<T>.Failure(Error.Create("RETRY_EXHAUSTED", "Retry attempts exhausted", 503));
    }
}



