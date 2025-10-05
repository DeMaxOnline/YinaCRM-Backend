// VO: EnvironmentName (local)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.ClientEnvironment.VOs;

/// <summary>
/// Human-readable environment name (e.g., "production", "staging-eu").
/// Normalization: trims and collapses internal whitespace to single spaces.
/// Validation: 1–64 chars; letters, digits, space, '-', '_', '.'.
/// </summary>
public readonly partial record struct EnvironmentName
{
    internal string Value { get; }
    private EnvironmentName(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<EnvironmentName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<EnvironmentName>.Failure(EnvironmentNameErrors.Empty());
        var s = Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length is < 1 or > 64 || !Allowed().IsMatch(s))
            return Result<EnvironmentName>.Failure(EnvironmentNameErrors.Invalid());
        return Result<EnvironmentName>.Success(new EnvironmentName(s));
    }

    [GeneratedRegex(@"^[\p{L}0-9 ._\-]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex Allowed();
}

public static class EnvironmentNameErrors
{
    public static Error Empty() => Error.Create("ENV_NAME_EMPTY", "Environment name is required", 400);
    public static Error Invalid() => Error.Create("ENV_NAME_INVALID", "Environment name must be 1–64 chars (letters, digits, space, . _ -)", 400);
}



