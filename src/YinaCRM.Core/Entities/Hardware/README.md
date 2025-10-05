Hardware Entity

Purpose
- Represents hardware synced from an external upstream API.
- Holds snapshot fields (serial/brand/model/IP/etc.), linkage to a Client, and remote identifiers/codes.

Invariants
- ExternalHardwareId, HardwareTypeCode, and HardwareDetailTypeCode are required.
- Snapshot updates are only allowed when the hardware is linked to a Client.
- No raw string properties; string-shaped data are value objects.
- Timestamps are stored in UTC (CreatedAt; UpdatedAt/LastSeenAt set via operations).

Properties
- Id: `HardwareId` (StrongId)
- ClientId?: `ClientId`
- ExternalHardwareId: `ExternalHardwareId`
- TypeCode: `HardwareTypeCode`
- DetailTypeCode: `HardwareDetailTypeCode`
- SerialNumber?: `SerialNumber`
- Brand?: `Brand`
- Model?: `Model`
- IpCom?: `IpAddress`
- WarrantyDate?: `DateOnly`
- DeliveredByUs: `bool`
- AnyDeskId?: `AnyDeskId`
- AnyDeskPassword?: `Secret`
- LastSeenAt?: `DateTime` (UTC)
- CreatedAt: `DateTime` (UTC)
- UpdatedAt?: `DateTime` (UTC)

Events
- `HardwareLinkedToClient` — when associated to a `ClientId`.
- `HardwareUnlinkedFromClient` — when association is removed.
- `HardwareSnapshotUpdated` — when snapshot fields are updated.

API Notes
- Factory and mutators return `Result`/`Result<T>`.
- Updates via `UpdateSnapshot(...)` enforce client linkage.
- Domain events are buffered; call `DequeueEvents()` to pull and clear.


