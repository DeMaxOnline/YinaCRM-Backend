ClientEnvironment Aggregate

Purpose
- Represents a named environment for a Client (e.g., production, staging), with typed absolute URLs and optional credentials/notes.

Invariants
- Name is required and validated by local VO `EnvironmentName`.
- Each `EnvUrl` uses typed `UrlTypeCode` and absolute `Url` VO (no raw strings).
- At most one primary (`IsPrimary=true`) per `UrlTypeCode` within an environment.
- UTC timestamps for `CreatedAt`; `UpdatedAt` set on changes.

Properties
- Id: `ClientEnvironmentId`
- ClientId: `ClientId`
- Name: `EnvironmentName`
- Description?: `Description`
- Urls: `IReadOnlyCollection<EnvUrl>`
- Username?: `Username`
- Password?: `Secret`
- Notes?: `Body`
- CreatedAt: `DateTime` (UTC)
- UpdatedAt?: `DateTime` (UTC)

EnvUrl
- Id: `Guid`
- TypeCode: `UrlTypeCode`
- Url: `Url` (absolute)
- IsPrimary: `bool`

Events
- `EnvironmentUrlAdded`
- `EnvironmentUrlUpdated`
- `EnvironmentUrlRemoved`

API Notes
- Factory and mutators return `Result`/`Result<T>`.
- Add/Update enforce the single-primary-per-type rule.
- Domain events are buffered; use `DequeueEvents()` to pull and clear.


