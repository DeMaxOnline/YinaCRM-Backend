using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.SupportTicket.Events;

public sealed record SupportTicketClosed(
    SupportTicketId TicketId,
    string? ClosureReason
) : DomainEventBase(TicketId.ToString(), nameof(SupportTicket))
{
}
