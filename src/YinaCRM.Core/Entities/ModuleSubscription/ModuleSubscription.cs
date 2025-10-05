using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;
using YinaCRM.Core.Abstractions;
using YinaCRM.Core.Entities.ModuleSubscription.Events;
using YinaCRM.Core.Events;
using YinaCRM.Core.ValueObjects;
namespace YinaCRM.Core.Entities.ModuleSubscription;
/// <summary>
/// Billing subscription for a client and module. No persistence concerns included.
/// </summary>
public sealed class ModuleSubscription : AggregateRoot<ModuleSubscriptionId>
{
    private ModuleSubscription()
    {
        // Required by EF Core
    }
    private ModuleSubscription(
        ModuleSubscriptionId id,
        ClientId clientId,
        ModuleName moduleName,
        PlanName? planName,
        bool active,
        DateOnly? startDateInvoice,
        DateOnly? renewalDate,
        int quantity,
        Money? unitPrice,
        DateTime createdAtUtc)
    {
        Id = id;
        ClientId = clientId;
        ModuleName = moduleName;
        PlanName = planName;
        Active = active;
        StartDateInvoice = startDateInvoice;
        RenewalDate = renewalDate;
        Quantity = quantity;
        UnitPrice = unitPrice;
        CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        UpdatedAt = null;
    }
    public override ModuleSubscriptionId Id { get; protected set; }
    public ClientId ClientId { get; private set; }
    public ModuleName ModuleName { get; private set; }
    public PlanName? PlanName { get; private set; }
    public bool Active { get; private set; }
    public DateOnly? StartDateInvoice { get; private set; }
    public DateOnly? RenewalDate { get; private set; }
    public int Quantity { get; private set; }
    public Money? UnitPrice { get; private set; }
    public static Result<ModuleSubscription> Create(
        ClientId clientId,
        ModuleName moduleName,
        PlanName? planName = null,
        bool active = false,
        DateOnly? startDateInvoice = null,
        DateOnly? renewalDate = null,
        int quantity = 0,
        Money? unitPrice = null,
        DateTime? createdAtUtc = null)
        => Create(
            ModuleSubscriptionId.New(),
            clientId,
            moduleName,
            planName,
            active,
            startDateInvoice,
            renewalDate,
            quantity,
            unitPrice,
            createdAtUtc);
    public static Result<ModuleSubscription> Create(
        ModuleSubscriptionId id,
        ClientId clientId,
        ModuleName moduleName,
        PlanName? planName = null,
        bool active = false,
        DateOnly? startDateInvoice = null,
        DateOnly? renewalDate = null,
        int quantity = 0,
        Money? unitPrice = null,
        DateTime? createdAtUtc = null)
    {
        if (quantity < 0) return Result<ModuleSubscription>.Failure(Errors.QuantityNegative());
        var sub = new ModuleSubscription(
            id,
            clientId,
            moduleName,
            planName,
            active,
            startDateInvoice,
            renewalDate,
            quantity,
            unitPrice,
            createdAtUtc ?? DateTime.UtcNow);
        return Result<ModuleSubscription>.Success(sub);
    }
    public Result Activate()
    {
        if (Active) return Result.Success();
        Active = true;
        RaiseEvent(new ModuleSubscriptionActivated(Id));
        return Result.Success();
    }
    public Result Deactivate()
    {
        if (!Active) return Result.Success();
        Active = false;
        RaiseEvent(new ModuleSubscriptionDeactivated(Id));
        return Result.Success();
    }
    public Result Change(ModuleName moduleName, PlanName? planName, int quantity, Money? unitPrice, DateOnly? startDateInvoice, DateOnly? renewalDate)
    {
        if (quantity < 0) return Result.Failure(Errors.QuantityNegative());
        var changed = false;
        if (!ModuleName.Equals(moduleName)) { ModuleName = moduleName; changed = true; }
        if (!Equals(PlanName, planName)) { PlanName = planName; changed = true; }
        if (Quantity != quantity) { Quantity = quantity; changed = true; }
        if (!Equals(UnitPrice, unitPrice)) { UnitPrice = unitPrice; changed = true; }
        if (!Equals(StartDateInvoice, startDateInvoice)) { StartDateInvoice = startDateInvoice; changed = true; }
        if (!Equals(RenewalDate, renewalDate)) { RenewalDate = renewalDate; changed = true; }
        if (!changed) return Result.Success();
        RaiseEvent(new ModuleSubscriptionChanged(Id, ModuleName, PlanName, Quantity, UnitPrice, StartDateInvoice, RenewalDate));
        return Result.Success();
    }
    /// <summary>
    /// Applies events to rebuild the aggregate state during event sourcing replay.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    protected override void ApplyEvent(IDomainEvent @event)
    {
        switch (@event)
        {
            case ModuleSubscriptionActivated activated:
                Active = true;
                UpdatedAt = activated.OccurredAtUtc;
                break;
            case ModuleSubscriptionDeactivated deactivated:
                Active = false;
                UpdatedAt = deactivated.OccurredAtUtc;
                break;
            case ModuleSubscriptionChanged changed:
                ModuleName = changed.ModuleName;
                PlanName = changed.PlanName;
                Quantity = changed.Quantity;
                UnitPrice = changed.UnitPrice;
                StartDateInvoice = changed.StartDateInvoice;
                RenewalDate = changed.RenewalDate;
                UpdatedAt = changed.OccurredAtUtc;
                break;
            default:
                // Unknown event type - this is acceptable for forward compatibility
                break;
        }
    }
    private static class Errors
    {
        public static Error QuantityNegative() => Error.Create("SUBSCRIPTION_QTY_NEGATIVE", "Quantity cannot be negative", 400);
    }
}



