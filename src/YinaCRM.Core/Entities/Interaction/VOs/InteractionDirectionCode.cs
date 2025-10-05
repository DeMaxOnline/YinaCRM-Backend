using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Interaction.VOs;

/// <summary>
/// Value object representing interaction direction with allowed values: inbound, outbound
/// </summary>
public readonly struct InteractionDirectionCode : IEquatable<InteractionDirectionCode>
{
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "inbound",
        "outbound"
    };

    // Predefined instances for convenience
    public static readonly InteractionDirectionCode Inbound = new("inbound");
    public static readonly InteractionDirectionCode Outbound = new("outbound");

    private readonly string? _value;

    private InteractionDirectionCode(string value) => _value = value?.ToLowerInvariant();

    public string Value => _value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

    public static Result<InteractionDirectionCode> Create(string? value)
        => TryCreate(value);

    public static Result<InteractionDirectionCode> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<InteractionDirectionCode>.Failure(InteractionDirectionCodeErrors.Required());

        value = value.Trim().ToLowerInvariant();

        if (!AllowedCodes.Contains(value))
            return Result<InteractionDirectionCode>.Failure(InteractionDirectionCodeErrors.InvalidCode(value));

        return Result<InteractionDirectionCode>.Success(new InteractionDirectionCode(value));
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedCodes.Contains(value.Trim().ToLowerInvariant());
    }

    public static IReadOnlyCollection<string> GetAllowedCodes() => AllowedCodes.ToList().AsReadOnly();

    public bool Equals(InteractionDirectionCode other) => _value == other._value;
    public override bool Equals(object? obj) => obj is InteractionDirectionCode other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public override string ToString() => Value;

    public static bool operator ==(InteractionDirectionCode left, InteractionDirectionCode right) => left.Equals(right);
    public static bool operator !=(InteractionDirectionCode left, InteractionDirectionCode right) => !left.Equals(right);
}

internal static class InteractionDirectionCodeErrors
{
    public static Error Required() => Error.Create("INTERACTION_DIRECTION_REQUIRED", "Interaction direction is required", 400);
    
    public static Error InvalidCode(string value) => Error.Create("INTERACTION_DIRECTION_INVALID", $"Invalid interaction direction '{value}'. Allowed values: inbound, outbound", 400);
}


