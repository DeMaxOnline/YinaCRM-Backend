using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class UrlErrors
{
    public static Error Empty() => Error.Create("URL_EMPTY", "Url is required", 400);
    public static Error Invalid() => Error.Create("URL_INVALID", "Url must be absolute with scheme and host", 400);
}



