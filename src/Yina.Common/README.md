# Yina.Common

Reusable primitives: StrongId, Result/Result<T>, Errors, Serialization, Resilience, Caching, and Protocol abstractions.

## Modules
- Foundation.Ids: StrongId<TTag> typed identifiers.
- Abstractions.Results: Result/Result<T> with map/bind/ensure helpers.
- Abstractions.Errors: rich Error with code/message/status/metadata.
- Serialization: JSON defaults, StrongId/DateOnly/TimeOnly converters.
- Resilience: simple retry executor with classifier/backoff hooks (outer layers).
- Caching: small in-memory cache abstraction.
- Protocols: message interfaces (IMessage, ICommand, IEvent, IQuery) for transport layers.

## Guidelines
- No throwing for expected control flow; return Result.
- Use Error.Create(code, message, status) to produce consistent errors.
- Leverage ResultExtensions to compose complex flows.
- Keep concrete transport/persistence concerns out of Common.

See: /docs/EngineeringPrinciples.md, /docs/PavedPath.md.
