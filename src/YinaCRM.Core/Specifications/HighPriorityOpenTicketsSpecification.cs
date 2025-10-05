using System.Linq.Expressions;
using YinaCRM.Core.Entities.SupportTicket;
using YinaCRM.Core.Entities.SupportTicket.VOs;

namespace YinaCRM.Core.Specifications;

public sealed class HighPriorityOpenTicketsSpecification : Specification<SupportTicket>
{
    public override Expression<Func<SupportTicket, bool>> ToExpression()
        => ticket =>
            ticket.Status != TicketStatusCode.Closed &&
            (ticket.Priority == TicketPriorityCode.High || ticket.Priority == TicketPriorityCode.Urgent);
}
