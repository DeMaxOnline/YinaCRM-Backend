using System;
using System.Net;

namespace Yina.Common.Abstractions.Errors;

/// <summary>High-level classification for errors.</summary>
public enum ErrorCategory
{
    /// <summary>No category.</summary>
    None = 0,
    /// <summary>Validation failure.</summary>
    Validation,
    /// <summary>Resource missing.</summary>
    NotFound,
    /// <summary>Conflict with existing state.</summary>
    Conflict,
    /// <summary>Concurrency violation.</summary>
    Concurrency,
    /// <summary>Authentication required.</summary>
    Unauthorized,
    /// <summary>Access denied.</summary>
    Forbidden,
    /// <summary>Precondition not satisfied.</summary>
    PreconditionFailed,
    /// <summary>Client is being rate limited.</summary>
    RateLimited,
    /// <summary>Timeout occurred.</summary>
    Timeout,
    /// <summary>External dependency failure.</summary>
    ExternalDependency,
    /// <summary>Business rule failure.</summary>
    BusinessRule,
    /// <summary>Infrastructure failure.</summary>
    Infrastructure,
    /// <summary>Transient failure.</summary>
    Transient,
    /// <summary>Unknown category.</summary>
    Unknown
}

/// <summary>Extensions to inspect error categories and characteristics.</summary>
public static class ErrorExtensions
{
    /// <summary>Determines whether the error represents a 4xx client error.</summary>
    public static bool IsClientError(this Error error)
        => error.StatusCode is >= 400 and < 500;

    /// <summary>Determines whether the error represents a 5xx server error.</summary>
    public static bool IsServerError(this Error error)
        => error.StatusCode >= 500;

    /// <summary>Determines whether the error relates to security (401/403).</summary>
    public static bool IsSecurityError(this Error error)
        => error.GetCategory() is ErrorCategory.Unauthorized or ErrorCategory.Forbidden;

    /// <summary>Determines whether the error is safe to retry later.</summary>
    public static bool IsTransient(this Error error)
    {
        if (error.GetCategory() is ErrorCategory.Transient or ErrorCategory.RateLimited or ErrorCategory.Timeout)
        {
            return true;
        }

        return error.StatusCode is (int)HttpStatusCode.TooManyRequests
            or (int)HttpStatusCode.RequestTimeout
            or 499
            or (int)HttpStatusCode.BadGateway
            or (int)HttpStatusCode.ServiceUnavailable
            or (int)HttpStatusCode.GatewayTimeout;
    }

    /// <summary>Determines whether the error should trigger a retry.</summary>
    public static bool IsRetryable(this Error error)
    {
        if (error.IsTransient())
        {
            return true;
        }

        return error.StatusCode is >= 500 and < 600 and not (int)HttpStatusCode.NotImplemented;
    }

    /// <summary>Maps the error to an <see cref="ErrorCategory"/>.</summary>
    public static ErrorCategory GetCategory(this Error error)
    {
        if (error.IsNone)
        {
            return ErrorCategory.None;
        }

        if (error.StatusCode != 0)
        {
            return error.StatusCode switch
            {
                (int)HttpStatusCode.BadRequest => ErrorCategory.Validation,
                (int)HttpStatusCode.Unauthorized => ErrorCategory.Unauthorized,
                (int)HttpStatusCode.Forbidden => ErrorCategory.Forbidden,
                (int)HttpStatusCode.NotFound => ErrorCategory.NotFound,
                (int)HttpStatusCode.Conflict => ErrorCategory.Conflict,
                (int)HttpStatusCode.PreconditionFailed => ErrorCategory.PreconditionFailed,
                (int)HttpStatusCode.TooManyRequests => ErrorCategory.RateLimited,
                (int)HttpStatusCode.RequestTimeout => ErrorCategory.Timeout,
                (int)HttpStatusCode.BadGateway or (int)HttpStatusCode.ServiceUnavailable or (int)HttpStatusCode.GatewayTimeout
                    => ErrorCategory.ExternalDependency,
                >= 500 and < 600 => ErrorCategory.Infrastructure,
                >= 400 and < 500 => ErrorCategory.BusinessRule,
                _ => ErrorCategory.Unknown
            };
        }

        var code = error.Code?.Trim() ?? string.Empty;
        if (StartsWith(code, "VALIDATION")) return ErrorCategory.Validation;
        if (StartsWith(code, "NOT_FOUND")) return ErrorCategory.NotFound;
        if (StartsWith(code, "ALREADY_EXISTS") || StartsWith(code, "CONFLICT")) return ErrorCategory.Conflict;
        if (StartsWith(code, "CONCURRENCY")) return ErrorCategory.Concurrency;
        if (StartsWith(code, "UNAUTHORIZED")) return ErrorCategory.Unauthorized;
        if (StartsWith(code, "FORBIDDEN")) return ErrorCategory.Forbidden;
        if (StartsWith(code, "PRECONDITION_FAILED")) return ErrorCategory.PreconditionFailed;
        if (StartsWith(code, "RATE_LIMITED") || StartsWith(code, "TOO_MANY_REQUESTS")) return ErrorCategory.RateLimited;
        if (StartsWith(code, "TIMEOUT")) return ErrorCategory.Timeout;
        if (StartsWith(code, "EXTERNAL") || StartsWith(code, "DEPENDENCY")) return ErrorCategory.ExternalDependency;
        if (StartsWith(code, "TRANSIENT")) return ErrorCategory.Transient;
        if (StartsWith(code, "INFRASTRUCTURE")) return ErrorCategory.Infrastructure;
        if (StartsWith(code, "BUSINESS_RULE")) return ErrorCategory.BusinessRule;
        return ErrorCategory.Unknown;
    }

    private static bool StartsWith(string code, string prefix)
        => code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}


