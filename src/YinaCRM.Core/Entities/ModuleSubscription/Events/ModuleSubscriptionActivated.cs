using YinaCRM.Core.Events;

namespace YinaCRM.Core.Entities.ModuleSubscription.Events;

public sealed record ModuleSubscriptionActivated(
    ModuleSubscriptionId SubscriptionId) : DomainEventBase(SubscriptionId.ToString(), nameof(ModuleSubscription))
{
}


