using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;

namespace YinaCRM.Core.Entities.ModuleSubscription.Events;

public sealed record ModuleSubscriptionChanged(
    ModuleSubscriptionId SubscriptionId,
    ModuleName ModuleName,
    PlanName? PlanName,
    int Quantity,
    Money? UnitPrice,
    DateOnly? StartDateInvoice,
    DateOnly? RenewalDate) : DomainEventBase(SubscriptionId.ToString(), nameof(ModuleSubscription))
{
}


