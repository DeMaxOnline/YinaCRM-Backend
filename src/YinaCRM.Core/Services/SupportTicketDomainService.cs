using System;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Entities.Interaction;
using YinaCRM.Core.Entities.SupportTicket;
using YinaCRM.Core.Entities.SupportTicket.VOs;

namespace YinaCRM.Core.Services;

public sealed class SupportTicketDomainService
{
    public Result LinkTicketToInteraction(SupportTicket ticket, Interaction interaction, string relatedType, Guid relatedId)
    {
        if (ticket.Status == TicketStatusCode.Closed)
        {
            return Result.Failure(SupportTicketServiceErrors.CannotLinkClosedTicket());
        }

        if (interaction == null)
        {
            return Result.Failure(SupportTicketServiceErrors.InteractionRequired());
        }

        var linkResult = interaction.AddLink(relatedType, relatedId);
        if (linkResult.IsFailure)
        {
            return Result.Failure(linkResult.Error);
        }

        return Result.Success();
    }

    private static class SupportTicketServiceErrors
    {
        public static Error CannotLinkClosedTicket() => Error.Create("DOMAIN_SERVICE_TICKET_CLOSED", "Cannot link interactions to a closed support ticket", 409);
        public static Error InteractionRequired() => Error.Create("DOMAIN_SERVICE_INTERACTION_REQUIRED", "Interaction instance is required", 400);
    }
}
