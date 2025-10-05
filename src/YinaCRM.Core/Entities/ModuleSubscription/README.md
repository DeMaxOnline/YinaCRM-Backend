ModuleSubscription Aggregate

Purpose
- Represents a client's subscription to a module, including plan, quantity, price, and dates.

Properties
- Id: `ModuleSubscriptionId`
- ClientId: `ClientId`
- ModuleName: `ModuleName`
- PlanName?: `PlanName`
- Active: `bool`
- StartDateInvoice?: `DateOnly`
- RenewalDate?: `DateOnly`
- Quantity: `int` (>= 0)
- UnitPrice?: `Money`
- CreatedAt: `DateTime` (UTC)
- UpdatedAt?: `DateTime` (UTC)

Rules
- Quantity must be non-negative.
- No raw strings; uses shared value objects for names and price.

Events
- `ModuleSubscriptionActivated`
- `ModuleSubscriptionDeactivated`
- `ModuleSubscriptionChanged`

API Notes
- Factory and mutators return `Result`/`Result<T>`.
- `Activate`/`Deactivate` are idempotent and emit respective events when state changes.
- `Change` updates details and emits `ModuleSubscriptionChanged` when any field changes.


