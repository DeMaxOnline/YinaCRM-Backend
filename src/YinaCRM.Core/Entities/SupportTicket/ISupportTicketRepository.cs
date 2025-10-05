using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.SupportTicket.VOs;

namespace YinaCRM.Core.Entities.SupportTicket;

/// <summary>
/// Repository interface for SupportTicket aggregate root.
/// Provides domain-specific operations for SupportTicket persistence.
/// </summary>
public interface ISupportTicketRepository : IRepository<SupportTicket, SupportTicketId>
{
    /// <summary>
    /// Retrieves a support ticket by its unique ticket number.
    /// </summary>
    Task<SupportTicket?> GetByNumberAsync(TicketNumber number, CancellationToken cancellationToken = default);
}
