using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class DescriptionErrors
{
    public static Error Empty() => Error.Create("DESC_EMPTY", "Description is required", 400);
    public static Error TooLong() => Error.Create("DESC_TOO_LONG", "Description must be at most 1000 characters", 400);
}



