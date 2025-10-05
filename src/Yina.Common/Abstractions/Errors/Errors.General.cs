namespace Yina.Common.Abstractions.Errors;

using System;
using System.Collections.Generic;

public static partial class Errors
{
    public static Error Failure(string code, string message, int statusCode = 500, IDictionary<string, string>? metadata = null)
        => Error.Create(code, message, statusCode, null, metadata);

    public static Error FromException(Exception exception, string? code = null, int statusCode = 500, IDictionary<string, string>? metadata = null)
    {
        var details = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);

        details["exception"] = exception.GetType().Name;
        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            details["stackTrace"] = exception.StackTrace!;
        }

        return Error.Create(code ?? "UNHANDLED_EXCEPTION", exception.Message, statusCode, null, details);
    }
}
