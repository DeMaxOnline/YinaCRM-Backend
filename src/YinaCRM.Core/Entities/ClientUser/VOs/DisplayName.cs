// VO: DisplayName (ClientUser - local)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.ClientUser.VOs;

/// <summary>
/// Human-readable display name for a client user.
/// Normalization: trims and collapses internal whitespace to single spaces.
/// Validation: 1–100 characters; printable letters/digits/space basic punctuation.
/// </summary>
public readonly partial record struct DisplayName
{
    internal string Value { get; }
    private DisplayName(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<DisplayName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<DisplayName>.Failure(DisplayNameErrors.Empty());
        var s = Regex.Replace(input.Trim(), "\\s+", " ");
        if (!Pattern().IsMatch(s) || s.Length > 100)
            return Result<DisplayName>.Failure(DisplayNameErrors.Invalid());
        return Result<DisplayName>.Success(new DisplayName(s));
    }

    [GeneratedRegex(@"^[\p{L}0-9 .,'\-]{1,100}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}

public static class DisplayNameErrors
{
    public static Error Empty() => Error.Create("CLIENTUSER_DISPLAYNAME_EMPTY", "Display name is required", 400);
    public static Error Invalid() => Error.Create("CLIENTUSER_DISPLAYNAME_INVALID", "Display name must be 1–100 chars (letters/digits/space . , ' -)", 400);
}


