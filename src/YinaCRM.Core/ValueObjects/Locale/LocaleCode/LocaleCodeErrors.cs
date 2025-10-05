using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class LocaleCodeErrors
{
    public static Error Empty() => Error.Create("LOCALE_EMPTY", "Locale code is required", 400);
    public static Error Invalid() => Error.Create("LOCALE_INVALID", "Unknown locale code", 400);
}



