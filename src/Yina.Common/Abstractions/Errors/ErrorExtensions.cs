using System;
using System.Net;

namespace Yina.Common.Abstractions.Errors;

public enum ErrorCategory
{
    None = 0,
    Validation,
    NotFound,
    Conflict,
    Concurrency,
    Unauthorized,
    Forbidden,
    PreconditionFailed,
    RateLimited,
    Timeout,
    ExternalDependency,
    BusinessRule,
    Infrastructure,
    Transient,
    Unknown
}

public static class ErrorExtensions
{
    public static bool IsClientError(this Error error)
        => error.StatusCode is >= 400 and < 500;

    public static bool IsServerError(this Error error)
        => error.StatusCode >= 500;

    public static bool IsSecurityError(this Error error)
        => error.GetCategory() is ErrorCategory.Unauthorized or ErrorCategory.Forbidden;

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

    public static bool IsRetryable(this Error error)
    {
        if (error.IsTransient())
        {
            return true;
        }

        return error.StatusCode is >= 500 and < 600 and not (int)HttpStatusCode.NotImplemented;
    }

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
