namespace Yina.Common.Abstractions.Errors;

using System.Collections.Generic;

public static partial class Errors
{
    public static Error Validation(string code, string message, string? field = null, IDictionary<string, string>? metadata = null)
        => Error.Create(NormalizeCode(code), message, 400, field, metadata);
}

