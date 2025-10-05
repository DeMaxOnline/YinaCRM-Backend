using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Identity.SecretVO;

public static class SecretErrors
{
    public static Error Empty() => Error.Create("SECRET_EMPTY", "Secret cannot be empty", 400);
}



