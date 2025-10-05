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
            hardwareId: null,
            assignedToUserId: null,
            status: TicketStatusCode.New,
            priority: TicketPriorityCode.Normal,
            slaDueAt: DateTime.UtcNow.AddHours(4),
            toBill: false));

        var created = Assert.IsType<SupportTicketCreated>(ticket.DequeueEvents().Single());
        Assert.Equal("T-2025-000001", created.Number.ToString());

        var assignee = UserId.New();
        Assert.True(ticket.AssignTo(assignee).IsSuccess);
        Assert.IsType<SupportTicketAssigned>(ticket.DequeueEvents().Single());

        // Assigning same user is no-op
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

        // Closed ticket cannot be modified
        Assert.True(ticket.AssignTo(UserId.New()).IsFailure);
        Assert.True(ticket.ChangePriority(TicketPriorityCode.High).IsFailure);
        Assert.True(ticket.UpdateSubject(DomainTestHelper.Title("New subject")).IsFailure);
        Assert.True(ticket.UpdateDescription(DomainTestHelper.Body("another")).IsFailure);
        Assert.True(ticket.LinkToHardware(HardwareId.New()).IsFailure);

        // Reopen to in_progress
        Assert.True(ticket.ChangeStatus(TicketStatusCode.InProgress).IsSuccess);
        Assert.IsType<SupportTicketStatusChanged>(ticket.DequeueEvents().Single());
        Assert.Null(ticket.ClosedAt);

        // Invalid transition
        var invalidTransition = ticket.ChangeStatus(TicketStatusCode.New);
        Assert.True(invalidTransition.IsFailure);
        Assert.Equal("TICKET_STATUS_INVALID_TRANSITION", invalidTransition.Error.Code);

        // Update mutable data
        Assert.True(ticket.ChangePriority(TicketPriorityCode.High).IsSuccess);
        Assert.True(ticket.UpdateSubject(DomainTestHelper.Title("Escalated")).IsSuccess);
        Assert.True(ticket.UpdateDescription(DomainTestHelper.Body("Escalated description")).IsSuccess);
        Assert.True(ticket.SetBilling(true).IsSuccess);
        Assert.True(ticket.UpdateSlaDueDate(DateTime.UtcNow.AddHours(2)).IsSuccess);
        Assert.True(ticket.LinkToHardware(HardwareId.New()).IsSuccess);

        // Verify no unexpected events remain
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

    private static TicketNumber CreateTicketNumber(string value)
        => DomainTestHelper.ExpectValue(TicketNumber.TryCreate(value));
}
