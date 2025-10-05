using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.SupportTicket.VOs;

/// <summary>
/// Value object representing a ticket priority code with allowed values: low, normal, high, urgent
/// </summary>
public readonly struct TicketPriorityCode : IEquatable<TicketPriorityCode>
{
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "low",
        "normal",
        "high",
        "urgent"
    };

    // Predefined priority codes for convenience
    public static readonly TicketPriorityCode Low = new("low");
    public static readonly TicketPriorityCode Normal = new("normal");
    public static readonly TicketPriorityCode High = new("high");
    public static readonly TicketPriorityCode Urgent = new("urgent");

    private readonly string? _value;

    private TicketPriorityCode(string value) => _value = value?.ToLowerInvariant();

    public string Value => _value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

    public static Result<TicketPriorityCode> Create(string? value)
        => TryCreate(value);

    public static Result<TicketPriorityCode> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TicketPriorityCode>.Failure(TicketPriorityCodeErrors.Required());

        value = value.Trim().ToLowerInvariant();

        if (!AllowedCodes.Contains(value))
            return Result<TicketPriorityCode>.Failure(TicketPriorityCodeErrors.InvalidCode(value));

        return Result<TicketPriorityCode>.Success(new TicketPriorityCode(value));
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedCodes.Contains(value.Trim().ToLowerInvariant());
    }

    public static IReadOnlyCollection<string> GetAllowedCodes() => AllowedCodes.ToList().AsReadOnly();

    public int GetSortOrder()
    {
        return _value switch
        {
            "urgent" => 0,
            "high" => 1,
            "normal" => 2,
            "low" => 3,
            _ => 99
        };
    }

    public bool IsHighPriority() => _value is "urgent" or "high";

    public bool Equals(TicketPriorityCode other) => _value == other._value;
    public override bool Equals(object? obj) => obj is TicketPriorityCode other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public override string ToString() => Value;

    public static bool operator ==(TicketPriorityCode left, TicketPriorityCode right) => left.Equals(right);
    public static bool operator !=(TicketPriorityCode left, TicketPriorityCode right) => !left.Equals(right);
}

internal static class TicketPriorityCodeErrors
{
    public static Error Required() => Error.Create("TICKET_PRIORITY_REQUIRED", "Ticket priority is required", 400);
    
    public static Error InvalidCode(string value) => Error.Create("TICKET_PRIORITY_INVALID", $"Invalid ticket priority '{value}'. Allowed values: low, normal, high, urgent", 400);
}


