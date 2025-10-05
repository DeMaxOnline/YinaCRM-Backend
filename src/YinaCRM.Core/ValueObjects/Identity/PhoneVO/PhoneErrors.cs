using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Identity.PhoneVO;

public static class PhoneErrors
{
    public static Error Empty() => Error.Create("PHONE_EMPTY", "Phone number is required", 400);
    public static Error E164Invalid() => Error.Create("PHONE_E164_INVALID", "E.164 phone must have + and 8–15 digits", 400);
    public static Error LocalInvalid() => Error.Create("PHONE_LOCAL_INVALID", "Local phone must have 8–15 digits", 400);
}



