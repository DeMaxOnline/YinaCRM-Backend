using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Interaction.VOs;

/// <summary>
/// Value object representing interaction type with allowed values: call, email, meeting, chat, remote
/// </summary>
public readonly struct InteractionTypeCode : IEquatable<InteractionTypeCode>
{
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "call",
        "email",
        "meeting",
        "chat",
        "remote"
    };

    // Predefined instances for convenience
    public static readonly InteractionTypeCode Call = new("call");
    public static readonly InteractionTypeCode Email = new("email");
    public static readonly InteractionTypeCode Meeting = new("meeting");
    public static readonly InteractionTypeCode Chat = new("chat");
    public static readonly InteractionTypeCode Remote = new("remote");

    private readonly string? _value;

    private InteractionTypeCode(string value) => _value = value?.ToLowerInvariant();

    public string Value => _value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

    public static Result<InteractionTypeCode> Create(string? value)
        => TryCreate(value);

    public static Result<InteractionTypeCode> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<InteractionTypeCode>.Failure(InteractionTypeCodeErrors.Required());

        value = value.Trim().ToLowerInvariant();

        if (!AllowedCodes.Contains(value))
            return Result<InteractionTypeCode>.Failure(InteractionTypeCodeErrors.InvalidCode(value));

        return Result<InteractionTypeCode>.Success(new InteractionTypeCode(value));
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedCodes.Contains(value.Trim().ToLowerInvariant());
    }

    public static IReadOnlyCollection<string> GetAllowedCodes() => AllowedCodes.ToList().AsReadOnly();

    public bool Equals(InteractionTypeCode other) => _value == other._value;
    public override bool Equals(object? obj) => obj is InteractionTypeCode other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public override string ToString() => Value;

    public static bool operator ==(InteractionTypeCode left, InteractionTypeCode right) => left.Equals(right);
    public static bool operator !=(InteractionTypeCode left, InteractionTypeCode right) => !left.Equals(right);
}

internal static class InteractionTypeCodeErrors
{
    public static Error Required() => Error.Create("INTERACTION_TYPE_REQUIRED", "Interaction type is required", 400);
    
    public static Error InvalidCode(string value) => Error.Create("INTERACTION_TYPE_INVALID", $"Invalid interaction type '{value}'. Allowed values: call, email, meeting, chat, remote", 400);
}





