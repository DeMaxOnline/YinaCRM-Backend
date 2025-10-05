namespace Yina.Common.Abstractions.Errors;

using System.Collections.Generic;

public static partial class Errors
{
    public static Error Unauthorized(string code, string message, IDictionary<string, string>? metadata = null)
        => Error.Create(NormalizeCode(code), message, 401, null, metadata);

    public static Error Forbidden(string code, string message, IDictionary<string, string>? metadata = null)
        => Error.Create(NormalizeCode(code), message, 403, null, metadata);
}

