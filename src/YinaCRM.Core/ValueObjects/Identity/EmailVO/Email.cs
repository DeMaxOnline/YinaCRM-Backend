// Placeholder VO: Email (shared)
#nullable enable
using Yina.Common.Abstractions.Results;
using System.Text.RegularExpressions;

namespace YinaCRM.Core.ValueObjects.Identity.EmailVO;

/// <summary>
/// Email address value object.
/// Normalization: trims and lowercases the address.
/// Validation: basic RFC-like shape user@host.tld (no spaces, has one '@', TLD length ≥ 2).
/// Does not throw on validation failures; use <see cref="TryCreate"/>.
/// </summary>
public readonly partial record struct Email
{
    private static readonly Regex Pattern = AllowedEmail();

    internal string Value { get; }

    private Email(string value) => Value = value;

    public override string ToString() => Value;

    public static Result<Email> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Email>.Failure(EmailErrors.Empty());

        var normalized = input.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
            return Result<Email>.Failure(EmailErrors.Invalid());

        return Result<Email>.Success(new Email(normalized));
    }

    [GeneratedRegex("^[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AllowedEmail();
}


