using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Core.ValueObjects.Identity.UsernameVO;

public static class UsernameErrors
{
    public static Error Empty() => Error.Create("USERNAME_EMPTY", "Username is required", 400);
    public static Error Invalid() => Error.Create("USERNAME_INVALID", "Username must be 3–50 chars: a-z, 0-9, '.', '_' or '-'", 400);
}



