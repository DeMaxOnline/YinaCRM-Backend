using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using ClientUserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.ClientUser.ClientUserIdTag>;

using YinaCRM.Core.Entities.Interaction;
using YinaCRM.Core.Entities.SupportTicket;
using YinaCRM.Core.Entities.SupportTicket.VOs;
using YinaCRM.Core.Services;

namespace YinaCRM.Core.Tests;

public sealed class SupportTicketDomainServiceTests
{
    private readonly SupportTicketDomainService _service = new();

    [Fact]
    public void CannotLinkClosedTicket()
    {
        var ticket = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000020"),
            DomainTestHelper.Title("Printer offline")));

        ticket.ChangeStatus(TicketStatusCode.Closed);
        var interaction = DomainTestHelper.ExpectValue(Interaction.Create(
            DomainTestHelper.InteractionType(),
            DomainTestHelper.InteractionDirection(),
            DomainTestHelper.Title()));

        var result = _service.LinkTicketToInteraction(ticket, interaction, "SupportTicket", ticket.Id.Value);
        Assert.True(result.IsFailure);
        Assert.Equal("DOMAIN_SERVICE_TICKET_CLOSED", result.Error.Code);
    }

    [Fact]
    public void RequiresInteractionInstance()
    {
        var ticket = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000021"),
            DomainTestHelper.Title("Printer offline")));

        var result = _service.LinkTicketToInteraction(ticket, null!, "SupportTicket", ticket.Id.Value);
        Assert.True(result.IsFailure);
        Assert.Equal("DOMAIN_SERVICE_INTERACTION_REQUIRED", result.Error.Code);
    }

    [Fact]
    public void AddsLinkWhenInvariantsSatisfied()
    {
        var ticket = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000022"),
            DomainTestHelper.Title("Printer offline")));

        var interaction = DomainTestHelper.ExpectValue(Interaction.Create(
            DomainTestHelper.InteractionType(),
            DomainTestHelper.InteractionDirection(),
            DomainTestHelper.Title()));

        var result = _service.LinkTicketToInteraction(ticket, interaction, "SupportTicket", ticket.Id.Value);
        Assert.True(result.IsSuccess);
        Assert.Single(interaction.Links);
    }

    private static TicketNumber CreateTicketNumber(string value)
        => DomainTestHelper.ExpectValue(TicketNumber.TryCreate(value));
}
