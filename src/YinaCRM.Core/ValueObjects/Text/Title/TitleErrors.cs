using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class TitleErrors
{
    public static Error Empty() => Error.Create("TITLE_EMPTY", "Title is required", 400);
    public static Error TooLong() => Error.Create("TITLE_TOO_LONG", "Title must be at most 200 characters", 400);
}



