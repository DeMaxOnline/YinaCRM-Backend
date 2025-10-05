Client Aggregate

Purpose
- Represents a customer (client) within the CRM domain.
- Encapsulates identity, naming, primary contact info, basic address fields, and tags.
- Emits domain events for lifecycle and key changes; no persistence concerns included.

Invariants
- InternalName is required and validated (lowercase, 3–64, [a-z0-9-]).
- YinaYinaId is required and must be > 0 (uniqueness is enforced by persistence later).
- CreatedAt is stored in UTC; UpdatedAt is set on mutating operations.
- No raw string properties are exposed; shared/local Value Objects are used.

Properties
- Id: `ClientId` (StrongId)
- YinaYinaId: `int`
- InternalName: `InternalName` (local VO)
- CompanyName?: `CompanyName`
- CommercialName?: `CommercialName`
- PrimaryEmail?: `Email`
- PrimaryPhone?: `Phone`
- AddressLine1?: `AddressLine`
- AddressLine2?: `AddressLine`
- City?: `City`
- PostalCode?: `PostalCode`
- Country?: `CountryName`
- Tags: `IReadOnlyCollection<Tag>`
- CreatedAt: `DateTime` (UTC)
- UpdatedAt?: `DateTime` (UTC)

Events
- `ClientCreated` — emitted by factory on successful creation.
- `ClientRenamed` — emitted when InternalName changes.
- `ClientPrimaryEmailChanged` — emitted when PrimaryEmail changes.

API Notes
- Factory/mutators return `Result`/`Result<T>` from Yina.Common.
- Domain events are buffered internally; call `DequeueEvents()` to pull and clear.


