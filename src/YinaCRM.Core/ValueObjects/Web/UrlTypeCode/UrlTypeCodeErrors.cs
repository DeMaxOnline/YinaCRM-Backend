using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects;

public static class UrlTypeCodeErrors
{
    public static Error Empty() => Error.Create("URLTYPE_EMPTY", "Url type code is required", 400);
    public static Error Invalid() => Error.Create("URLTYPE_INVALID", "Url type code must be lowercase alphanumeric with hyphens (max 64)", 400);
}



