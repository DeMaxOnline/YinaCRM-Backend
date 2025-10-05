using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Identity.ContactVO;

public static class ContactErrors
{
    public static Error Empty() => Error.Create("CONTACT_EMPTY", "At least one of email or phone must be provided", 400);
}


