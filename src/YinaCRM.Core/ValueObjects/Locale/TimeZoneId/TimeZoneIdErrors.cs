using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class TimeZoneIdErrors
{
    public static Error Empty() => Error.Create("TZ_EMPTY", "Time zone ID is required", 400);
    public static Error Invalid() => Error.Create("TZ_INVALID", "Unknown time zone ID", 400);
}



