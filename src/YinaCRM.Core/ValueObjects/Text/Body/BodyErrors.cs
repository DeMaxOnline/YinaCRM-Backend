using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class BodyErrors
{
    public static Error Empty() => Error.Create("BODY_EMPTY", "Body is required", 400);
    public static Error TooLong() => Error.Create("BODY_TOO_LONG", "Body must be at most 20000 characters", 400);
}



