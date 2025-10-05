using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.SupportTicket.Events;

public sealed record SupportTicketAssigned(
    SupportTicketId TicketId,
    UserId? PreviousAssigneeId,
    UserId NewAssigneeId
) : DomainEventBase(TicketId.ToString(), nameof(SupportTicket))
{
}
