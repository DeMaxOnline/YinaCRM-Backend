using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.SupportTicket.VOs;

/// <summary>
/// Value object representing a unique ticket number with format T-YYYY-NNNNNN
/// </summary>
public readonly partial struct TicketNumber : IEquatable<TicketNumber>
{
    private readonly string? _value;

    private TicketNumber(string value) => _value = value;

    public string Value => _value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

    public static Result<TicketNumber> Create(string? value)
        => TryCreate(value);

    public static Result<TicketNumber> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TicketNumber>.Failure(TicketNumberErrors.Required());

        value = value.Trim().ToUpperInvariant();

        if (!IsValidFormat(value))
            return Result<TicketNumber>.Failure(TicketNumberErrors.InvalidFormat(value));

        return Result<TicketNumber>.Success(new TicketNumber(value));
    }

    public static Result<TicketNumber> Generate(int sequenceNumber, DateTime? createdAt = null)
    {
        var date = createdAt ?? DateTime.UtcNow;
        var year = date.Year;
        
        if (sequenceNumber < 1 || sequenceNumber > 999999)
            return Result<TicketNumber>.Failure(TicketNumberErrors.InvalidSequenceNumber(sequenceNumber));

        var number = $"T-{year:0000}-{sequenceNumber:000000}";
        return Result<TicketNumber>.Success(new TicketNumber(number));
    }

    private static bool IsValidFormat(string value)
    {
        return TicketNumberRegex().IsMatch(value);
    }

    [GeneratedRegex(@"^T-\d{4}-\d{6}$", RegexOptions.Compiled)]
    private static partial Regex TicketNumberRegex();

    public bool Equals(TicketNumber other) => _value == other._value;
    public override bool Equals(object? obj) => obj is TicketNumber other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public override string ToString() => Value;

    public static bool operator ==(TicketNumber left, TicketNumber right) => left.Equals(right);
    public static bool operator !=(TicketNumber left, TicketNumber right) => !left.Equals(right);
}

internal static class TicketNumberErrors
{
    public static Error Required() => Error.Create("TICKET_NUMBER_REQUIRED", "Ticket number is required", 400);
    
    public static Error InvalidFormat(string value) => Error.Create("TICKET_NUMBER_INVALID_FORMAT", $"Ticket number '{value}' must match format T-YYYY-NNNNNN", 400);
    
    public static Error InvalidSequenceNumber(int number) => Error.Create("TICKET_NUMBER_INVALID_SEQUENCE", $"Sequence number {number} must be between 1 and 999999", 400);
}


