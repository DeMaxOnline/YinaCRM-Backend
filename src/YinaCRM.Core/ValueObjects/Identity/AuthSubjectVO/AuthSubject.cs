// VO: AuthSubject (shared)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects.Identity.AuthSubjectVO;

/// <summary>
/// Authentication subject identifier (e.g., sub claim). Accepts common provider formats.
/// Normalization: trims; preserves case.
/// Validation: 1–256 characters from a permissive set.
/// </summary>
public readonly partial record struct AuthSubject
{
    internal string Value { get; }
    private AuthSubject(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<AuthSubject> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<AuthSubject>.Failure(AuthSubjectErrors.Empty());
        var s = input.Trim();
        if (s.Length > 256 || !Allowed().IsMatch(s))
            return Result<AuthSubject>.Failure(AuthSubjectErrors.Invalid());
        return Result<AuthSubject>.Success(new AuthSubject(s));
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-:@./|+=]{1,256}$", RegexOptions.Compiled)]
    private static partial Regex Allowed();
}

public static class AuthSubjectErrors
{
    public static Error Empty() => Error.Create("AUTHSUB_EMPTY", "Auth subject is required", 400);
    public static Error Invalid() => Error.Create("AUTHSUB_INVALID", "Auth subject contains invalid characters or length", 400);
}



