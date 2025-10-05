// Placeholder VO: AddressLine (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.AddressVO.AddressLineVO;

/// <summary>
/// Address line value object (e.g., street and number).
/// Normalization: trims; collapses internal whitespace to single spaces.
/// Validation: 1–200 characters; printable characters, no control chars.
/// </summary>
public readonly partial record struct AddressLine
{
    internal string Value { get; }
    private AddressLine(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<AddressLine> TryCreate(string? input)
    {
        var norm = Normalize(input);
        if (string.IsNullOrWhiteSpace(norm))
            return Result<AddressLine>.Failure(AddressLineErrors.Empty());
        if (norm.Length > 200 || !ValidChars().IsMatch(norm))
            return Result<AddressLine>.Failure(AddressLineErrors.Invalid());
        return Result<AddressLine>.Success(new AddressLine(norm));
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim();
        return Regex.Replace(s, "\\s+", " ");
    }

    [GeneratedRegex(@"^[\t\x20-\x7E\p{L}\p{N}.,'#/\\-]{1,200}$", RegexOptions.Compiled)]
    private static partial Regex ValidChars();
}

