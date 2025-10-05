using System;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using ClientUserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.ClientUser.ClientUserIdTag>;
using YinaCRM.Core.Entities.Client;
using YinaCRM.Core.Entities.SupportTicket;
using YinaCRM.Core.Entities.SupportTicket.VOs;
using YinaCRM.Core.Specifications;
using YinaCRM.Core.Entities.Client.VOs;

namespace YinaCRM.Core.Tests;



public sealed class SpecificationTests
{
    [Fact]
    public void ActiveClientSpecification_EvaluatesDeletedFlag()
    {
        var client = DomainTestHelper.ExpectValue(Client.Create(100, DomainTestHelper.InternalName()));
        var spec = new ActiveClientSpecification();
        Assert.True(spec.IsSatisfiedBy(client));

        // soft-delete via reflection to simulate archived client
        var method = typeof(Client).BaseType!.GetMethod("SoftDelete", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method!.Invoke(client, new object?[] { null });
        Assert.False(spec.IsSatisfiedBy(client));
    }

    [Fact]
    public void HighPriorityOpenTicketsSpecification_CombinesCorrectly()
    {
        var openHigh = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000010"),
            DomainTestHelper.Title("Network outage"),
            priority: TicketPriorityCode.High));

        var closedUrgent = DomainTestHelper.ExpectValue(SupportTicket.Create(
            ClientId.New(),
            ClientUserId.New(),
            CreateTicketNumber("T-2025-000011"),
            DomainTestHelper.Title("Disk failure"),
            priority: TicketPriorityCode.Urgent));
        closedUrgent.ChangeStatus(TicketStatusCode.Closed);

        var spec = new HighPriorityOpenTicketsSpecification();
        Assert.True(spec.IsSatisfiedBy(openHigh));
        Assert.False(spec.IsSatisfiedBy(closedUrgent));
    }

    [Fact]
    public void Specification_Combinators_Work()
    {
        var client = DomainTestHelper.ExpectValue(Client.Create(100, DomainTestHelper.InternalName()));
        var active = new ActiveClientSpecification();
        var nameSpec = new ClientNameSpecification(DomainTestHelper.InternalName());

        var combined = active.And(nameSpec);
        Assert.True(combined.IsSatisfiedBy(client));

        var negated = active.Not();
        Assert.False(negated.IsSatisfiedBy(client));
    }

    private static TicketNumber CreateTicketNumber(string value)
        => DomainTestHelper.ExpectValue(TicketNumber.TryCreate(value));

    private sealed class ClientNameSpecification : Specification<Client>
    {
        private readonly InternalName _expected;

        public ClientNameSpecification(InternalName expected) => _expected = expected;

        public override System.Linq.Expressions.Expression<Func<Client, bool>> ToExpression()
            => client => client.InternalName.Equals(_expected);
    }
}

