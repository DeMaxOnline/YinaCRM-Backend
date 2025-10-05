# YinaCRM.Core

Domain Core for Yina CRM. Pure domain model with aggregates, value objects, domain events, specifications, and domain services. No infrastructure dependencies.

## Key Concepts
- Aggregates: extend AggregateRoot<TId> with StrongId identifiers and UTC audit fields.
- Value Objects: immutable, normalized, validated via TryCreate, with typed error codes.
- Domain Events: derive from DomainEventBase and are stamped with AggregateVersion at raise-time.
- Specifications: composable predicates for repository queries.
- Domain Services: encapsulate cross-aggregate rules.

## Invariants
- No raw strings/guids exposed across boundaries; always use VOs/StrongIds.
- Mutations return Result/Result<T>; no exceptions for expected validation failures.
- All DateTime are UTC. UpdatedAt is null on creation; subsequent events set it to the event timestamp.

## Extensibility
- Add VOs under ValueObjects/* or per-entity VOs/.
- Add events under Entities/<Aggregate>/Events.
- Add specs in Specifications/.
- Add domain services in Services/.

## Persistence Guidance (Outer Layer)
- Map VOs as owned types; use StrongId converters.
- Use optimistic concurrency on Version.
- Implement outbox for domain events; publish with stamped version/time.

## Testing
- Builders under Builders/ accept primitives and convert to VOs immediately.
- Use replay (LoadFromHistory) to validate aggregates from event streams.

See: /docs/CoreRules.md, /docs/PavedPath.md, /docs/EngineeringPrinciples.md.
