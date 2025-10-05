using YinaCRM.Core.Abstractions;

namespace YinaCRM.Core.Entities.ModuleSubscription;

/// <summary>
/// Repository interface for ModuleSubscription aggregate root.
/// Provides persistence operations for ModuleSubscription.
/// </summary>
public interface IModuleSubscriptionRepository : IRepository<ModuleSubscription, ModuleSubscriptionId>
{
    // No additional methods - queries for subscriptions by client or module
    // should be handled by query services following CQRS
}
