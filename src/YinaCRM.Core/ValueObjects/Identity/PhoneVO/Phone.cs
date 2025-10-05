// Placeholder VO: Phone (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.Identity.PhoneVO;

/// <summary>
/// Phone number value object.
/// Normalization: removes spaces, dashes, dots, parentheses; preserves leading '+' when present.
/// Validation: E.164 (+ and 8–15 digits) or local digits (8–15) without '+'.
/// Does not throw on validation failures; use <see cref="TryCreate"/>.
/// </summary>
public readonly partial record struct Phone
{
    internal string Value { get; }
    public bool IsE164 => Value.StartsWith('+');

    private Phone(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Phone> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Phone>.Failure(PhoneErrors.Empty());

        var raw = Normalize(input);
        if (raw.StartsWith('+'))
        {
            var digits = raw[1..];
            if (digits.Length is < 8 or > 15 || !AllowedDigits().IsMatch(digits))
                return Result<Phone>.Failure(PhoneErrors.E164Invalid());
        }
        else
        {
            if (raw.Length is < 8 or > 15 || !AllowedDigits().IsMatch(raw))
                return Result<Phone>.Failure(PhoneErrors.LocalInvalid());
        }

        return Result<Phone>.Success(new Phone(raw));
    }

    private static string Normalize(string input)
    {
        input = input.Trim();
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsDigit(ch)) sb.Append(ch);
            else if (ch == '+' && sb.Length == 0) sb.Append(ch);
        }
        return sb.ToString();
    }

    [GeneratedRegex("^[0-9]+$", RegexOptions.Compiled)]
    private static partial Regex AllowedDigits();
}


