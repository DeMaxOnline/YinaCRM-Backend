using YinaCRM.Core.Entities.SupportTicket.VOs;
using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.SupportTicket.Events;

public sealed record SupportTicketCreated(
    SupportTicketId TicketId,
    ClientId ClientId,
    ClientUserId CreatedByUserId,
    TicketNumber Number,
    TicketStatusCode Status,
    TicketPriorityCode Priority,
    DateTime CreatedAt
) : DomainEventBase(TicketId.ToString(), nameof(SupportTicket))
{
}
