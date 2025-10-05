using System;
using System.Linq;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using ClientUserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.ClientUser.ClientUserIdTag>;
using HardwareId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Hardware.HardwareIdTag>;
using SupportTicketId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.SupportTicketIdTag>;
using UserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.User.UserIdTag>;
using YinaCRM.Core.Entities.SupportTicket;
using YinaCRM.Core.Entities.SupportTicket.Events;
using YinaCRM.Core.Entities.SupportTicket.VOs;

namespace YinaCRM.Core.Tests;

public sealed class SupportTicketTests
{
    [Fact]
    public void FullTicketLifecycle()
    {
        var ticket = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000001"),
            DomainTestHelper.Title("Printer offline"),
            DomainTestHelper.Body("The printer is not responding"),
            priority: TicketPriorityCode.High));

        var created = Assert.IsType<SupportTicketCreated>(ticket.DequeueEvents().Single());
        Assert.Equal("T-2025-000001", created.Number.ToString());

        var assignee = UserId.New();
        Assert.True(ticket.AssignTo(assignee).IsSuccess);
        Assert.IsType<SupportTicketAssigned>(ticket.DequeueEvents().Single());

        Assert.True(ticket.AssignTo(assignee).IsSuccess);
        Assert.Empty(ticket.DequeueEvents());

        Assert.True(ticket.ChangeStatus(TicketStatusCode.InProgress).IsSuccess);
        Assert.IsType<SupportTicketStatusChanged>(ticket.DequeueEvents().Single());

        Assert.True(ticket.ChangeStatus(TicketStatusCode.Resolved).IsSuccess);
        Assert.IsType<SupportTicketStatusChanged>(ticket.DequeueEvents().Single());

        Assert.True(ticket.ChangeStatus(TicketStatusCode.Closed).IsSuccess);
        var closedEvents = ticket.DequeueEvents().ToArray();
        Assert.Contains(closedEvents, e => e is SupportTicketStatusChanged);
        Assert.Contains(closedEvents, e => e is SupportTicketClosed);
        Assert.NotNull(ticket.ClosedAt);

        Assert.True(ticket.AssignTo(UserId.New()).IsFailure);
        Assert.True(ticket.ChangePriority(TicketPriorityCode.Low).IsFailure);
        Assert.True(ticket.UpdateSubject(DomainTestHelper.Title("New subject")).IsFailure);
        Assert.True(ticket.UpdateDescription(DomainTestHelper.Body("another")).IsFailure);
        Assert.True(ticket.LinkToHardware(HardwareId.New()).IsFailure);

        Assert.True(ticket.ChangeStatus(TicketStatusCode.InProgress).IsSuccess);
        Assert.IsType<SupportTicketStatusChanged>(ticket.DequeueEvents().Single());
        Assert.Null(ticket.ClosedAt);

        var invalidTransition = ticket.ChangeStatus(TicketStatusCode.New);
        Assert.True(invalidTransition.IsFailure);
        Assert.Equal("TICKET_STATUS_INVALID_TRANSITION", invalidTransition.Error.Code);

        Assert.True(ticket.ChangePriority(TicketPriorityCode.Urgent).IsSuccess);
        Assert.True(ticket.UpdateSubject(DomainTestHelper.Title("Escalated")).IsSuccess);
        Assert.True(ticket.UpdateDescription(DomainTestHelper.Body("Escalated description")).IsSuccess);
        Assert.True(ticket.SetBilling(true).IsSuccess);
        Assert.True(ticket.UpdateSlaDueDate(DateTime.UtcNow.AddHours(2)).IsSuccess);
        Assert.True(ticket.LinkToHardware(HardwareId.New()).IsSuccess);

        ticket.DequeueEvents();
    }

    [Fact]
    public void InvalidStatusTransitions_AreRejected()
    {
        var ticket = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000002"),
            DomainTestHelper.Title("Printer offline")));

        ticket.DequeueEvents();
        Assert.True(ticket.ChangeStatus(TicketStatusCode.Waiting).IsSuccess);
        var invalid = ticket.ChangeStatus(TicketStatusCode.New);
        Assert.True(invalid.IsFailure);
        Assert.Equal("TICKET_STATUS_INVALID_TRANSITION", invalid.Error.Code);

        Assert.True(ticket.ChangeStatus(TicketStatusCode.Closed).IsSuccess);
        var reopenInvalid = ticket.ChangeStatus(TicketStatusCode.Resolved);
        Assert.True(reopenInvalid.IsFailure);
        Assert.Equal("TICKET_STATUS_INVALID_TRANSITION", reopenInvalid.Error.Code);
    }

    [Fact]
    public void EventReplay_ReconstructsTicket()
    {
        var ticket = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-100001"),
            DomainTestHelper.Title("Printer offline"),
            DomainTestHelper.Body("Initial body"),
            priority: TicketPriorityCode.High));

        ticket.AssignTo(UserId.New());
        ticket.ChangeStatus(TicketStatusCode.InProgress);
        ticket.UpdateDescription(DomainTestHelper.Body("Investigating"));
        ticket.SetBilling(true);

        var events = ticket.DequeueEvents().ToArray();
        var rehydrated = (SupportTicket)Activator.CreateInstance(typeof(SupportTicket), nonPublic: true)!;
        rehydrated.LoadFromHistory(events);

        Assert.Equal(ticket.Priority, rehydrated.Priority);
        Assert.Equal(ticket.Status, rehydrated.Status);
        Assert.Equal(ticket.AssignedToUserId, rehydrated.AssignedToUserId);
        Assert.Equal(ticket.Number.ToString(), rehydrated.Number.ToString());
    }

    private static TicketNumber CreateTicketNumber(string value)
        => DomainTestHelper.ExpectValue(TicketNumber.TryCreate(value));
}








