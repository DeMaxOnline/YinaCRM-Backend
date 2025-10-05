// VO: RoleName (shared)
#nullable enable
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Role name for client users or permissions. Normalization: trims and collapses spaces.
/// Validation: 1–64 characters.
/// </summary>
public readonly record struct RoleName
{
    internal string Value { get; }
    private RoleName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<RoleName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<RoleName>.Failure(RoleNameErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 64)
            return Result<RoleName>.Failure(RoleNameErrors.TooLong());
        return Result<RoleName>.Success(new RoleName(s));
    }
}

public static class RoleNameErrors
{
    public static Error Empty() => Error.Create("ROLENAME_EMPTY", "Role name is required", 400);
    public static Error TooLong() => Error.Create("ROLENAME_TOO_LONG", "Role name must be at most 64 characters", 400);
}



