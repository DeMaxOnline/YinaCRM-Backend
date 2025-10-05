// Placeholder VO: ActorKindCode (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects.Codes.ActorKindCodeVO;

/// <summary>
/// Cross-cutting actor kind code.
/// Allowed values: User, ClientUser.
/// Normalization: trims and case-insensitive matching to canonical value.
/// </summary>
public readonly record struct ActorKindCode
{
    private static readonly string[] Allowed = new[] { "User", "ClientUser" };

    // Predefined instances for convenience
    public static readonly ActorKindCode User = new("User");
    public static readonly ActorKindCode ClientUser = new("ClientUser");

    internal string Value { get; }
    private ActorKindCode(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public static Result<ActorKindCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<ActorKindCode>.Failure(ActorKindCodeErrors.Empty());
        var s = input.Trim();
        foreach (var a in Allowed)
        {
            if (string.Equals(a, s, StringComparison.OrdinalIgnoreCase))
                return Result<ActorKindCode>.Success(new ActorKindCode(a));
        }
        return Result<ActorKindCode>.Failure(ActorKindCodeErrors.Invalid());
    }
}


