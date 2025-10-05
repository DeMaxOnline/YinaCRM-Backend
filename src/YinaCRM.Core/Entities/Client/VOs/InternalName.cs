// Client-specific VO: InternalName
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Client.VOs;

/// <summary>
/// Internal name for a client. Intended for stable, URL/slug-like identifiers.
/// Normalization: trims and lowercases.
/// Validation: 3–64 chars, pattern ^[a-z0-9-]+$.
/// </summary>
public readonly partial record struct InternalName
{
    internal string Value { get; }
    private InternalName(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<InternalName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<InternalName>.Failure(InternalNameErrors.Empty());

        var s = input.Trim().ToLowerInvariant();
        if (!Pattern().IsMatch(s))
            return Result<InternalName>.Failure(InternalNameErrors.Invalid());

        return Result<InternalName>.Success(new InternalName(s));
    }

    [GeneratedRegex("^[a-z0-9-]{3,64}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}

public static class InternalNameErrors
{
    public static Error Empty() => Error.Create("CLIENT_INTERNALNAME_EMPTY", "Internal name is required", 400);
    public static Error Invalid() => Error.Create("CLIENT_INTERNALNAME_INVALID", "Internal name must be 3–64 chars: lowercase alphanumeric and hyphens", 400);
}


