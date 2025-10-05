# ADR: Domain Event Versioning and UpdatedAt Semantics

## Status
Accepted

## Context
Aggregates raise rich domain events and the system relies on event sourcing-friendly metadata for auditing and replay. UpdatedAt timestamps drifted when set directly to DateTime.UtcNow, and events lacked aggregate version information.

## Decision
- Clone events deriving from DomainEventBase at raise time with AggregateVersion = Version + 1.
- Keep UpdatedAt null for the initial creation event and mirror OccurredAtUtc for subsequent events.
- Persist events via outbox using the stamped version/timestamp.

## Consequences
- Event consumers get deterministic versioning and aligned timestamps.
- Creation timestamps remain audit-friendly (no UpdatedAt unless a change occurs).
- Infrastructure must treat UpdatedAt as nullable and rely on domain events for change chronology.

