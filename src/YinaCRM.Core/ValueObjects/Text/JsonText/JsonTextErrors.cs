using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class JsonTextErrors
{
    public static Error Empty() => Error.Create("JSON_EMPTY", "JSON text is required", 400);
    public static Error Invalid() => Error.Create("JSON_INVALID", "JSON text is not well-formed", 400);
}



