namespace Yina.Common.Abstractions.Errors;

using System;
using System.Collections.Generic;

public static partial class Errors
{
    private const string DefaultUnexpectedMessage = "An unexpected error occurred.";

    public static Error Failure(string code, string message, int statusCode = 500, IDictionary<string, string>? metadata = null)
        => Error.Create(NormalizeCode(code), message, statusCode, null, metadata);

    public static Error FromException(Exception exception, string? code = null, int statusCode = 500, IDictionary<string, string>? metadata = null)
    {
        var details = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);

        details["exception"] = exception.GetType().Name;

#if DEBUG
        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            details["message"] = exception.Message;
        }

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            details["stackTrace"] = exception.StackTrace!;
        }
#endif

        var errorCode = code ?? "UNHANDLED_EXCEPTION";
        return Error.Create(errorCode, DefaultUnexpectedMessage, statusCode, null, details);
    }
}

