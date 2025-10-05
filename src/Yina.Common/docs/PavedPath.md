# Paved Path: Using Core/Common

## Common (Yina.Common)
- StrongId<TTag>: use StrongId<TTag>.New() for new ids; persist via converters.
- Result/Result<T>: return Result (no throwing) for validation/control flow; map errors with Error.Create.
- Errors: include Code, Message, StatusCode; do not leak internal exception details to callers.
- Serialization: use JsonDefaults with StrongId/DateOnly/TimeOnly converters.
- Resilience: use RetryExecutor for transient external calls (outer layers only).

## Core (YinaCRM.Core)
- Value Objects: implement TryCreate and normalization; errors via *Errors helpers; immutable records.
- Aggregates: extend AggregateRoot<TId>; raise events for meaningful changes; timestamps are UTC; UpdatedAt is null on creation.
- Domain Events: derive from DomainEventBase; payloads are StrongIds/VOs; Core stamps AggregateVersion.
- Specifications: derive from Specification<T> and compose queries safely.
- Domain Services: encapsulate cross-aggregate rules; keep aggregates focused.

## Patterns
- New VO: create a folder under ValueObjects or entity VOs/; add TryCreate, normalization, and *Errors.
- New Aggregate: create Entities/YourAggregate, id alias, events folder, factories + mutators returning Result.
- New Event: derive from DomainEventBase(aggregateId, nameof(Aggregate)); do not include raw strings/guids.
- New Spec: implement ToExpression() with LINQ-friendly logic; compose with .And/.Or/.Not.

## Persistence (Outer Layer)
- Use EF Core owned types for VOs; StrongId converters; optimistic concurrency on Version.
- Outbox pattern: persist dequeued domain events with stamped version/time; deliver asynchronously with idempotency.

## Testing
- Prefer domain-first tests; replay events to validate rehydration; use DomainTestHelper for VOs.
- Keep builders for ergonomic setup; verify both success and failure paths.

