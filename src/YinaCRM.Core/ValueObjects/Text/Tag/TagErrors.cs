using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class TagErrors
{
    public static Error Empty() => Error.Create("TAG_EMPTY", "Tag is required", 400);
    public static Error Invalid() => Error.Create("TAG_INVALID", "Tag must be lowercase alphanumeric with optional hyphens (max 50)", 400);
}



