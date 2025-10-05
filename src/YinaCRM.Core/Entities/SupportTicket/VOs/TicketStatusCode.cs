using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.SupportTicket.VOs;

/// <summary>
/// Value object representing a ticket status code with allowed values: new, in_progress, waiting, resolved, closed
/// </summary>
public readonly struct TicketStatusCode : IEquatable<TicketStatusCode>
{
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "new",
        "in_progress", 
        "waiting",
        "resolved",
        "closed"
    };

    // Predefined status codes for convenience
    public static readonly TicketStatusCode New = new("new");
    public static readonly TicketStatusCode InProgress = new("in_progress");
    public static readonly TicketStatusCode Waiting = new("waiting");
    public static readonly TicketStatusCode Resolved = new("resolved");
    public static readonly TicketStatusCode Closed = new("closed");

    private readonly string? _value;

    private TicketStatusCode(string value) => _value = value?.ToLowerInvariant();

    public string Value => _value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

    public static Result<TicketStatusCode> Create(string? value)
        => TryCreate(value);

    public static Result<TicketStatusCode> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TicketStatusCode>.Failure(TicketStatusCodeErrors.Required());

        value = value.Trim().ToLowerInvariant();

        if (!AllowedCodes.Contains(value))
            return Result<TicketStatusCode>.Failure(TicketStatusCodeErrors.InvalidCode(value));

        return Result<TicketStatusCode>.Success(new TicketStatusCode(value));
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedCodes.Contains(value.Trim().ToLowerInvariant());
    }

    public static IReadOnlyCollection<string> GetAllowedCodes() => AllowedCodes.ToList().AsReadOnly();

    public bool CanTransitionTo(TicketStatusCode newStatus)
    {
        // Define valid state transitions
        return (_value, newStatus._value) switch
        {
            ("new", "in_progress") => true,
            ("new", "waiting") => true,
            ("new", "resolved") => true,
            ("new", "closed") => true,
            
            ("in_progress", "waiting") => true,
            ("in_progress", "resolved") => true,
            ("in_progress", "closed") => true,
            
            ("waiting", "in_progress") => true,
            ("waiting", "resolved") => true,
            ("waiting", "closed") => true,
            
            ("resolved", "closed") => true,
            ("resolved", "in_progress") => true, // Allow reopening
            
            ("closed", "in_progress") => true, // Allow reopening
            
            _ => false
        };
    }

    public bool Equals(TicketStatusCode other) => _value == other._value;
    public override bool Equals(object? obj) => obj is TicketStatusCode other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public override string ToString() => Value;

    public static bool operator ==(TicketStatusCode left, TicketStatusCode right) => left.Equals(right);
    public static bool operator !=(TicketStatusCode left, TicketStatusCode right) => !left.Equals(right);
}

internal static class TicketStatusCodeErrors
{
    public static Error Required() => Error.Create("TICKET_STATUS_REQUIRED", "Ticket status is required", 400);
    
    public static Error InvalidCode(string value) => Error.Create("TICKET_STATUS_INVALID", $"Invalid ticket status '{value}'. Allowed values: new, in_progress, waiting, resolved, closed", 400);
    
    public static Error InvalidTransition(string from, string to) => Error.Create("TICKET_STATUS_INVALID_TRANSITION", $"Cannot transition ticket status from '{from}' to '{to}'", 400);
}


