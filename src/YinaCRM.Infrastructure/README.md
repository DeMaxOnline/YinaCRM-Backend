# YinaCRM.Infrastructure

Infrastructure layer for Yina CRM. Implements adapter modules (Auth0 identity, persistence, messaging, storage, caching, notifications, webhooks, secrets) behind clean interfaces. Use AddInfrastructure to register defaults and swap providers per environment.

## Highlights
- Auth0 integration (token verification, OAuth flows, management API metadata sync, webhook signatures).
- Postgres connection factory and outbox dispatcher hooks.
- Redis-backed distributed cache (RedisDistributedCache).
- RabbitMQ messaging adapters (RabbitMqMessagePublisher/RabbitMqMessageConsumer).
- Webhook dispatcher with retries & signing, HMAC signing service, secret store abstraction.
- Comprehensive abstractions in Abstractions/* for application layer consumption.
- Unit/contract tests under 	ests/YinaCRM.Infrastructure.Tests covering Auth0 adapters, Redis cache, RabbitMQ messaging, webhook dispatcher.

See docs/Infrastructure for architecture, setup guides, operational runbooks, and interface documentation.

