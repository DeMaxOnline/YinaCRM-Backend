// Placeholder VO: Username (shared)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.ValueObjects.Identity.UsernameVO;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Username value object.
/// Normalization: trims and lowercases.
/// Validation: 3–50 characters, alphanumeric plus '.', '_' and '-'.
/// </summary>
public readonly partial record struct Username
{
    internal string Value { get; }
    private Username(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Username> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Username>.Failure(UsernameErrors.Empty());
        var norm = input.Trim().ToLowerInvariant();
        if (!UsernamePattern().IsMatch(norm))
            return Result<Username>.Failure(UsernameErrors.Invalid());
        return Result<Username>.Success(new Username(norm));
    }

    [GeneratedRegex("^[a-z0-9._-]{3,50}$", RegexOptions.Compiled)]
    private static partial Regex UsernamePattern();
}


