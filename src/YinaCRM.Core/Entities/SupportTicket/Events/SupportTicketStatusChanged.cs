using YinaCRM.Core.Entities.SupportTicket.VOs;
using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.SupportTicket.Events;

public sealed record SupportTicketStatusChanged(
    SupportTicketId TicketId,
    TicketStatusCode OldStatus,
    TicketStatusCode NewStatus
) : DomainEventBase(TicketId.ToString(), nameof(SupportTicket))
{
}
