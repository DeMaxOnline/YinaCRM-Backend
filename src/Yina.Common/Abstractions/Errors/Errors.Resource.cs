namespace Yina.Common.Abstractions.Errors;

using System.Collections.Generic;

public static partial class Errors
{
    public static Error NotFound(string code, string message, IDictionary<string, string>? metadata = null)
        => Error.Create(code, message, 404, null, metadata);

    public static Error Conflict(string code, string message, IDictionary<string, string>? metadata = null)
        => Error.Create(code, message, 409, null, metadata);
}
