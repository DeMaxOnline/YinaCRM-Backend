using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class CommercialNameErrors
{
    public static Error Empty() => Error.Create("COMMERCIALNAME_EMPTY", "Commercial name is required", 400);
    public static Error TooLong() => Error.Create("COMMERCIALNAME_TOO_LONG", "Commercial name must be at most 200 characters", 400);
}



