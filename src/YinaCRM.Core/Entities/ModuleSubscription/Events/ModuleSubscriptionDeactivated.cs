using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.ModuleSubscription.Events;

public sealed record ModuleSubscriptionDeactivated(
    ModuleSubscriptionId SubscriptionId) : DomainEventBase(SubscriptionId.ToString(), nameof(ModuleSubscription))
{
}


