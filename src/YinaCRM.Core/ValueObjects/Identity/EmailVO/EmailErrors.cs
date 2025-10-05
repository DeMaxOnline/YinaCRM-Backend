using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Identity.EmailVO;

public static class EmailErrors
{
    public static Error Empty() => Error.Create("EMAIL_EMPTY", "Email is required", 400);
    public static Error Invalid() => Error.Create("EMAIL_INVALID", "Email format is invalid", 400);
}



