Note Aggregate

Purpose
- Free-form notes with visibility, pinning, and tags; links notes to other entities via NoteLink.

Properties
- Id: `NoteId`
- Body: `Body`
- Visibility: `VisibilityCode` (local; allowed: internal, shared)
- Pinned: `bool`
- Tags: `IReadOnlyCollection<Tag>`
- CreatedByKind: `ActorKindCode`
- CreatedById: `Guid`
- CreatedAt: `DateTime` (UTC)
- EditedAt?: `DateTime` (UTC)

NoteLink
- NoteId: `NoteId`
- RelatedType: `RelatedTypeCode`
- RelatedId: `Guid`
- Composite key: `NoteId + RelatedType + RelatedId`

Rules
- Body required (use VO `Body` to construct valid instances).
- Client users cannot create internal notes (Visibility=internal with CreatedByKind=ClientUser is rejected).

Events
- `NoteCreated`
- `NoteEdited`
- `NotePinned`
- `NoteUnpinned`

API Notes
- Factory and mutators return `Result`/`Result<T>`.
- Domain events are buffered; use `DequeueEvents()` pattern where integrating.


