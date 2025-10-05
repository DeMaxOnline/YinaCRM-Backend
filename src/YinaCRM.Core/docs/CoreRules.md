# Core Domain Rules

## Ground Principles
- Core remains infrastructure-free: no EF Core, HTTP, or IO concerns inside aggregates.
- Identifiers use StrongId aliases; external code never interacts with raw Guid identities.
- Textual data is always represented by value objects; aggregates never expose raw strings.
- Factories and mutators return `Result`/`Result<T>` and rely on VO `TryCreate` for validation.
- Successful state changes queue domain events that downstream layers can dispatch.
- All timestamps are stored in UTC; the domain is single-tenant by design.

## Domain Conventions

### Value Objects
- Provide `TryCreate(...)` constructors that return `Result<T>` without throwing for validation.
- Normalize inputs deterministically (trim, casing, canonical formatting) before validation.
- Enforce explicit length and pattern rules; code-like VOs often publish an allowlist.
- Implemented as light immutable record structs with companion `*Errors` helpers.

### Mutation & Error Handling
- Domain operations never throw for expected validation failures; they surface typed `Error` values (400/403/404/409, etc.).
- Builders accept primitives but immediately convert them to VOs; `Build()` returns `Result<TAggregate>`.

### Domain Events
- Creation and meaningful changes raise explicit events (e.g., `ClientCreated`, `ClientRenamed`).
- Event payloads use StrongIds/VOs plus UTC timestamps; no raw identifiers or strings.
- Aggregates buffer events in memory and expose `DequeueEvents()`; outbox publishing is an outer-layer concern.

### Time & Identity
- All `DateTime` values are stored/computed as `DateTimeKind.Utc`; use `DateOnly` for date-only semantics.
- Aggregate identifiers use StrongId aliases; child component IDs may stay as `Guid` when not aggregate identities.

### Other Conventions
- The domain is single tenant; multi-tenant scoping lives outside Core.
- Core models identity shapes (AuthSubject, ActorKindCode) but does not implement authorization.
- Specifications capture reusable business rules and can be combined for richer queries.

## Aggregate Rules

### Client
- Purpose: represents a customer with identity, contact info, address, and tags.
- Required invariants: `InternalName` VO (lowercase, length 3-64, [a-z0-9-]) and positive `YinaYinaId`.
- Data uses VOs for company names, email, phone, address lines, tags, etc.; timestamps in UTC.
- Events: `ClientCreated`, `ClientRenamed`, `ClientPrimaryEmailChanged` raised on successful operations.

### ClientEnvironment
- Purpose: captures a named client environment (production, staging) with typed URLs and optional credentials.
- Invariants: `EnvironmentName` VO required; URLs stored as `EnvUrl` entries with `UrlTypeCode` + absolute `Url`.
- Only one primary (`IsPrimary`) URL per type code; credentials and notes use secure VOs (`Username`, `Secret`, `Body`).
- Events: `EnvironmentUrlAdded`, `EnvironmentUrlUpdated`, `EnvironmentUrlRemoved`.

### ClientUser
- Purpose: client-facing user profile with display name, optional contact info, and role metadata.
- Uses VOs for `DisplayName`, `Email`, `Phone`, `RoleName`; audit timestamps in UTC.
- Factory/mutators validate inputs via VOs; updates return `Result`.

### User (Internal)
- Purpose: internal operator with auth subject, contact info, and locale preferences.
- Requires `AuthSubject`, `DisplayName`, `Email`; optional `TimeZoneId` and `LocaleCode` VOs.
- Profile updates rely on VO validation and return `Result`.

### Hardware
- Purpose: hardware assets synced from upstream systems, optionally linked to a client.
- Required fields: `ExternalHardwareId`, `HardwareTypeCode`, `HardwareDetailTypeCode`.
- Snapshot updates (serial, brand, model, IP, warranty) allowed only when linked to a client.
- Events: `HardwareLinkedToClient`, `HardwareUnlinkedFromClient`, `HardwareSnapshotUpdated`.

### ModuleSubscription
- Purpose: client subscription to a module with plan, quantity, pricing, and renewal info.
- Invariants: `Quantity` must be non-negative; names and price use VOs (`ModuleName`, `PlanName`, `Money`).
- Events: `ModuleSubscriptionActivated`, `ModuleSubscriptionDeactivated`, `ModuleSubscriptionChanged` when state shifts.

### SupportTicket
- Purpose: customer support request handling status, priority, assignment, SLA, and lifecycle events.
- Ticket number format: `T-YYYY-NNNNNN`; enforced via `TicketNumber` VO.
- Default status `new` and priority `normal`; closed tickets cannot be modified except via reopen.
- Valid status transitions:
  - `new` -> `in_progress`, `waiting`, `resolved`, `closed`
  - `in_progress` -> `waiting`, `resolved`, `closed`
  - `waiting` -> `in_progress`, `resolved`, `closed`
  - `resolved` -> `closed`, `in_progress`
  - `closed` -> `in_progress`
- Events: `SupportTicketCreated`, `SupportTicketAssigned`, `SupportTicketStatusChanged`, `SupportTicketClosed`.

### Note
- Purpose: free-form notes with visibility controls, pinning, tags, and links to related entities.
- Invariants: `Body` VO required; `VisibilityCode` supports `internal` or `shared`.
- Rule: client users cannot create internal-visibility notes.
- Events: `NoteCreated`, `NoteEdited`, `NotePinned`, `NoteUnpinned`.

## Specifications
- Specification pattern encapsulates business predicates (e.g., `ActiveClientSpecification`, `HighPriorityTicketSpecification`).
- Specifications are composable via AND/OR/NOT helpers and can translate to expressions for persistence queries.

### Persistence Mapping Guidance
- Map value objects as EF Core owned types or via custom converters; never expose primitive properties directly.
- Register `StrongId<TTag>` converters to persist identifiers as `uniqueidentifier`/`uuid` while keeping domain types strongly typed.
- Persist domain events via an outbox; infrastructure should read `DequeueEvents()` and stamp the stored `AggregateVersion`.
- For collection bridge partials (tags, URLs, participants), validate database records and throw descriptive exceptions when invalid data is encountered.
- Keep infrastructure concerns (DbContext, migrations) outside Core; repositories translate specifications to LINQ expressions for querying.

## Working With Core
- Create aggregates via factories/builders, immediately handling the `Result<T>` outcome.
- After mutations, call `DequeueEvents()` and dispatch via the application layer.
- Keep Core free of infrastructure dependencies; persistence, transport, and configuration belong to outer layers.


