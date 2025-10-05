using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Codes.RelatedTypeCodeVO;

public static class RelatedTypeCodeErrors
{
    public static Error Empty() => Error.Create("RELATEDTYPE_EMPTY", "Related type code is required", 400);
    public static Error Invalid() => Error.Create("RELATEDTYPE_INVALID", "Related type code is not allowed", 400);
}



