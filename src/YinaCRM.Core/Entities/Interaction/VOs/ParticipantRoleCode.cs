using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Interaction.VOs;

/// <summary>
/// Value object representing participant role with allowed values: organizer, attendee, cc, observer
/// </summary>
public readonly struct ParticipantRoleCode : IEquatable<ParticipantRoleCode>
{
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "organizer",
        "attendee",
        "cc",
        "observer"
    };

    // Predefined instances for convenience
    public static readonly ParticipantRoleCode Organizer = new("organizer");
    public static readonly ParticipantRoleCode Attendee = new("attendee");
    public static readonly ParticipantRoleCode Cc = new("cc");
    public static readonly ParticipantRoleCode Observer = new("observer");

    private readonly string? _value;

    private ParticipantRoleCode(string value) => _value = value?.ToLowerInvariant();

    public string Value => _value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

    public static Result<ParticipantRoleCode> Create(string? value)
        => TryCreate(value);

    public static Result<ParticipantRoleCode> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<ParticipantRoleCode>.Failure(ParticipantRoleCodeErrors.Required());

        value = value.Trim().ToLowerInvariant();

        if (!AllowedCodes.Contains(value))
            return Result<ParticipantRoleCode>.Failure(ParticipantRoleCodeErrors.InvalidCode(value));

        return Result<ParticipantRoleCode>.Success(new ParticipantRoleCode(value));
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedCodes.Contains(value.Trim().ToLowerInvariant());
    }

    public static IReadOnlyCollection<string> GetAllowedCodes() => AllowedCodes.ToList().AsReadOnly();

    public bool Equals(ParticipantRoleCode other) => _value == other._value;
    public override bool Equals(object? obj) => obj is ParticipantRoleCode other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public override string ToString() => Value;

    public static bool operator ==(ParticipantRoleCode left, ParticipantRoleCode right) => left.Equals(right);
    public static bool operator !=(ParticipantRoleCode left, ParticipantRoleCode right) => !left.Equals(right);
}

internal static class ParticipantRoleCodeErrors
{
    public static Error Required() => Error.Create("PARTICIPANT_ROLE_REQUIRED", "Participant role is required", 400);
    
    public static Error InvalidCode(string value) => Error.Create("PARTICIPANT_ROLE_INVALID", $"Invalid participant role '{value}'. Allowed values: organizer, attendee, cc, observer", 400);
}


