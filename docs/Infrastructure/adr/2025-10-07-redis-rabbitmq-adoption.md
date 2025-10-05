# ADR: Adopt Redis & RabbitMQ for Infrastructure Layer

## Status
Accepted – 2025-10-07

## Context
- YinaCRM needs durable messaging for domain/integration events and a low-latency cache for read models and idempotency tokens.
- Domain layer is infrastructure-agnostic; outer layer must hide provider specifics behind interfaces.
- Requirements:
  - Fan-out messaging with routing patterns, delivery acknowledgements, DLQ support.
  - Cache with per-tenant isolation, sliding/absolute expiration, and high availability.
  - Seamless integration with existing .NET stack and container-based dev workflow.
  - Production readiness (managed offerings, observability, retry semantics).

## Decision
- Use **RabbitMQ** (topic exchange) for messaging via `RabbitMqMessagePublisher`/`RabbitMqMessageConsumer`.
- Use **Redis** for distributed cache via `RedisDistributedCache` (StackExchange.Redis).
- Introduce `PostgresOutboxDispatcher` to bridge domain events and RabbitMQ, ensuring at-least-once delivery with persistence.
- Keep abstractions intact (`IMessagePublisher`, `IMessageConsumer`, `IDistributedCache`, `IOutboxDispatcher`) so alternative providers can be introduced later.

## Consequences
- Local dev requires Docker (Postgres, Redis, RabbitMQ). Testcontainers cover automated tests.
- Operations runbook must cover Redis/RabbitMQ health, outbox monitoring, and confirm behaviour.
- Publisher confirms + mandatory publish provide confidence but require tuning for high throughput.
- Cache TTL precedence (absolute > sliding > defaults) documented; sliding refresh test ensures behaviour.
- Outbox table schema defined; background worker must run in every environment.
- Future provider swaps (e.g., Azure Service Bus) only require new adapter + DI registration changes.
