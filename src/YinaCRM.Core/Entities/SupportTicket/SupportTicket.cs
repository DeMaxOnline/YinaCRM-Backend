using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.SupportTicket.Events;
using YinaCRM.Core.Entities.SupportTicket.VOs;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;
namespace YinaCRM.Core.Entities.SupportTicket;
/// <summary>
/// SupportTicket aggregate root (no persistence concerns).
/// Encapsulates ticket identity, status, priority, assignment and billing info.
/// Invariants enforced via factory/mutators returning Result/Result&lt;T&gt;.
/// </summary>
public sealed class SupportTicket : AggregateRoot<SupportTicketId>
{
    private SupportTicket()
    {
        // Required by EF Core
    }
    private SupportTicket(
        SupportTicketId id,
        ClientId clientId,
        HardwareId? hardwareId,
        ClientUserId createdByClientUserId,
        UserId? assignedToUserId,
        TicketNumber number,
        Title subject,
        Body? description,
        TicketStatusCode status,
        TicketPriorityCode priority,
        DateTime? slaDueAt,
        DateTime? closedAt,
        bool toBill,
        DateTime createdAt)
    {
        Id = id;
        ClientId = clientId;
        HardwareId = hardwareId;
        CreatedByClientUserId = createdByClientUserId;
        AssignedToUserId = assignedToUserId;
        Number = number;
        Subject = subject;
        Description = description;
        Status = status;
        Priority = priority;
        SlaDueAt = slaDueAt.HasValue ? DateTime.SpecifyKind(slaDueAt.Value, DateTimeKind.Utc) : null;
        ClosedAt = closedAt.HasValue ? DateTime.SpecifyKind(closedAt.Value, DateTimeKind.Utc) : null;
        ToBill = toBill;
        CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
        UpdatedAt = null;
    }
    public override SupportTicketId Id { get; protected set; }
    public ClientId ClientId { get; private set; }
    public HardwareId? HardwareId { get; private set; }
    public ClientUserId CreatedByClientUserId { get; private set; }
    public UserId? AssignedToUserId { get; private set; }
    public TicketNumber Number { get; private set; }
    public Title Subject { get; private set; }
    public Body? Description { get; private set; }
    public TicketStatusCode Status { get; private set; }
    public TicketPriorityCode Priority { get; private set; }
    public DateTime? SlaDueAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public bool ToBill { get; private set; }
    // Factory method generating a new SupportTicketId and ticket number
    public static Result<SupportTicket> Create(
        ClientId clientId,
        ClientUserId createdByClientUserId,
        TicketNumber number,
        Title subject,
        Body? description = null,
        HardwareId? hardwareId = null,
        UserId? assignedToUserId = null,
        TicketStatusCode? status = null,
        TicketPriorityCode? priority = null,
        DateTime? slaDueAt = null,
        bool toBill = false,
        DateTime? createdAt = null)
        => Create(
            SupportTicketId.New(),
            clientId,
            createdByClientUserId,
            number,
            subject,
            description,
            hardwareId,
            assignedToUserId,
            status,
            priority,
            slaDueAt,
            toBill,
            createdAt);
    // Factory method with explicit SupportTicketId
    public static Result<SupportTicket> Create(
        SupportTicketId id,
        ClientId clientId,
        ClientUserId createdByClientUserId,
        TicketNumber number,
        Title subject,
        Body? description = null,
        HardwareId? hardwareId = null,
        UserId? assignedToUserId = null,
        TicketStatusCode? status = null,
        TicketPriorityCode? priority = null,
        DateTime? slaDueAt = null,
        bool toBill = false,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(subject.Value))
            return Result<SupportTicket>.Failure(SupportTicketErrors.SubjectRequired());
        if (number.IsEmpty)
            return Result<SupportTicket>.Failure(SupportTicketErrors.TicketNumberRequired());
        // Use defaults if not provided
        var ticketStatus = status ?? TicketStatusCode.New;
        var ticketPriority = priority ?? TicketPriorityCode.Normal;
        var ticket = new SupportTicket(
            id,
            clientId,
            hardwareId,
            createdByClientUserId,
            assignedToUserId,
            number,
            subject,
            description,
            ticketStatus,
            ticketPriority,
            slaDueAt,
            null, // closedAt is null for new tickets
            toBill,
            createdAt ?? DateTime.UtcNow);
        ticket.RaiseEvent(new SupportTicketCreated(
            ticket.Id,
            ticket.ClientId,
            ticket.CreatedByClientUserId,
            ticket.Number,
            ticket.Status,
            ticket.Priority,
            ticket.CreatedAt));
        return Result<SupportTicket>.Success(ticket);
    }
    public Result AssignTo(UserId? newAssigneeId)
    {
        if (AssignedToUserId == newAssigneeId)
            return Result.Success();
        if (Status == TicketStatusCode.Closed)
            return Result.Failure(SupportTicketErrors.CannotModifyClosedTicket());
        var previousAssigneeId = AssignedToUserId;
        AssignedToUserId = newAssigneeId;
        if (newAssigneeId.HasValue)
        {
            RaiseEvent(new SupportTicketAssigned(
                Id,
                previousAssigneeId,
                newAssigneeId.Value));
        }
        return Result.Success();
    }
    public Result ChangeStatus(TicketStatusCode newStatus)
    {
        if (Status == newStatus)
            return Result.Success();
        if (!Status.CanTransitionTo(newStatus))
            return Result.Failure(SupportTicketErrors.InvalidStatusTransition(Status.Value, newStatus.Value));
        var oldStatus = Status;
        Status = newStatus;
        RaiseEvent(new SupportTicketStatusChanged(
            Id,
            oldStatus,
            newStatus));
        // Handle closed status
        if (newStatus == TicketStatusCode.Closed)
        {
            ClosedAt = UpdatedAt;
            RaiseEvent(new SupportTicketClosed(
                Id,
                null));
        }
        else if (oldStatus == TicketStatusCode.Closed)
        {
            // Reopening ticket
            ClosedAt = null;
        }
        return Result.Success();
    }
    public Result ChangePriority(TicketPriorityCode newPriority)
    {
        if (Priority == newPriority)
            return Result.Success();
        if (Status == TicketStatusCode.Closed)
            return Result.Failure(SupportTicketErrors.CannotModifyClosedTicket());
        Priority = newPriority;
        return Result.Success();
    }
    public Result UpdateSubject(Title newSubject)
    {
        if (string.IsNullOrWhiteSpace(newSubject.Value))
            return Result.Failure(SupportTicketErrors.SubjectRequired());
        if (Subject.Equals(newSubject))
            return Result.Success();
        if (Status == TicketStatusCode.Closed)
            return Result.Failure(SupportTicketErrors.CannotModifyClosedTicket());
        Subject = newSubject;
        return Result.Success();
    }
    public Result UpdateDescription(Body? newDescription)
    {
        if (Description.Equals(newDescription))
            return Result.Success();
        if (Status == TicketStatusCode.Closed)
            return Result.Failure(SupportTicketErrors.CannotModifyClosedTicket());
        Description = newDescription;
        return Result.Success();
    }
    public Result SetBilling(bool toBill)
    {
        if (ToBill == toBill)
            return Result.Success();
        ToBill = toBill;
        return Result.Success();
    }
    public Result UpdateSlaDueDate(DateTime? slaDueAt)
    {
        if (SlaDueAt == slaDueAt)
            return Result.Success();
        SlaDueAt = slaDueAt.HasValue ? DateTime.SpecifyKind(slaDueAt.Value, DateTimeKind.Utc) : null;
        return Result.Success();
    }
    public Result LinkToHardware(HardwareId? hardwareId)
    {
        if (HardwareId == hardwareId)
            return Result.Success();
        if (Status == TicketStatusCode.Closed)
            return Result.Failure(SupportTicketErrors.CannotModifyClosedTicket());
        HardwareId = hardwareId;
        return Result.Success();
    }
    /// <summary>
    /// Applies events to rebuild the aggregate state during event sourcing replay.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    protected override void ApplyEvent(IDomainEvent @event)
    {
        switch (@event)
        {
            case SupportTicketCreated created:
                Id = created.TicketId;
                ClientId = created.ClientId;
                CreatedByClientUserId = created.CreatedByUserId;
                Number = created.Number;
                Status = created.Status;
                Priority = created.Priority;
                CreatedAt = created.CreatedAt;
                break;
            case SupportTicketAssigned assigned:
                AssignedToUserId = assigned.NewAssigneeId;
                UpdatedAt = assigned.OccurredAtUtc;
                break;
            case SupportTicketStatusChanged statusChanged:
                Status = statusChanged.NewStatus;
                if (statusChanged.NewStatus == TicketStatusCode.Closed)
                {
                    ClosedAt = statusChanged.OccurredAtUtc;
                }
                else if (Status == TicketStatusCode.Closed && statusChanged.NewStatus != TicketStatusCode.Closed)
                {
                    ClosedAt = null; // Reopening ticket
                }
                UpdatedAt = statusChanged.OccurredAtUtc;
                break;
            case SupportTicketClosed closed:
                Status = TicketStatusCode.Closed;
                ClosedAt = closed.OccurredAtUtc;
                UpdatedAt = closed.OccurredAtUtc;
                break;
            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }
    private static class SupportTicketErrors
    {
        public static Error SubjectRequired() => Error.Create("TICKET_SUBJECT_REQUIRED", "Ticket subject is required", 400);
        public static Error TicketNumberRequired() => Error.Create("TICKET_NUMBER_REQUIRED", "Ticket number is required", 400);
        public static Error CannotModifyClosedTicket() => Error.Create("TICKET_CLOSED", "Cannot modify a closed ticket", 400);
        public static Error InvalidStatusTransition(string from, string to) => Error.Create("TICKET_STATUS_INVALID_TRANSITION", $"Cannot transition ticket status from '{from}' to '{to}'", 400);
    }
}



