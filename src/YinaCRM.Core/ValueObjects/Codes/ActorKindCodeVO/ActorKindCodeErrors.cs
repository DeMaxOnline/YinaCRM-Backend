using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

public static class ActorKindCodeErrors
{
    public static Error Empty() => Error.Create("ACTORKIND_EMPTY", "Actor kind code is required", 400);
    public static Error Invalid() => Error.Create("ACTORKIND_INVALID", "Actor kind code is not allowed", 400);
}



