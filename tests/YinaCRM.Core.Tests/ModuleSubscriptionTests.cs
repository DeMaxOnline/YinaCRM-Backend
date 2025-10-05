using System;
using ClientId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.Client.ClientIdTag>;
using YinaCRM.Core.Entities.ModuleSubscription;
using YinaCRM.Core.Entities.ModuleSubscription.Events;

namespace YinaCRM.Core.Tests;

public sealed class ModuleSubscriptionTests
{
    [Fact]
    public void ActivateDeactivateAndChangeSubscription()
    {
        var subscription = DomainTestHelper.ExpectValue(ModuleSubscription.Create(
            ClientId.New(),
            DomainTestHelper.ModuleName("crm"),
            DomainTestHelper.PlanName("pro"),
            active: false,
            startDateInvoice: DateOnly.FromDateTime(DateTime.UtcNow),
            renewalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            quantity: 5,
            unitPrice: DomainTestHelper.Money(25m)));

        Assert.True(subscription.Activate().IsSuccess);
        Assert.IsType<ModuleSubscriptionActivated>(subscription.DequeueEvents().Single());

        Assert.True(subscription.Deactivate().IsSuccess);
        Assert.IsType<ModuleSubscriptionDeactivated>(subscription.DequeueEvents().Single());

        Assert.True(subscription.Change(
            DomainTestHelper.ModuleName("crm"),
            DomainTestHelper.PlanName("enterprise"),
            10,
            DomainTestHelper.Money(30m),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2))).IsSuccess);
        Assert.IsType<ModuleSubscriptionChanged>(subscription.DequeueEvents().Single());

        // No changes => no event
        Assert.True(subscription.Change(
            subscription.ModuleName,
            subscription.PlanName,
            subscription.Quantity,
            subscription.UnitPrice,
            subscription.StartDateInvoice,
            subscription.RenewalDate).IsSuccess);
        Assert.Empty(subscription.DequeueEvents());
    }

    [Fact]
    public void NegativeQuantity_Fails()
    {
        var failure = ModuleSubscription.Create(
            ClientId.New(),
            DomainTestHelper.ModuleName(),
            quantity: -1);
        Assert.True(failure.IsFailure);
        Assert.Equal("SUBSCRIPTION_QTY_NEGATIVE", failure.Error.Code);
    }
}
