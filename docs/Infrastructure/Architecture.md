# Infrastructure Architecture Overview

## Layer Responsibilities
- **Infrastructure** isolates all side-effects (databases, identity, messaging, storage, telemetry) behind contracts. The domain (YinaCRM.Core) and any future application layer consume only abstractions.
- **Adapters** live under src/YinaCRM.Infrastructure/* (e.g., Auth/Auth0, Persistence, Messaging) and implement interfaces from src/YinaCRM.Infrastructure/Abstractions.
- **Cross-cutting services** (secrets, signing, webhook delivery, observability) are centralised and shared by feature-specific adapters.
- **Configuration** flows from strongly-typed options (Auth0Options, PostgresOptions, RedisOptions, RabbitMqOptions, etc.) validated on startup. All services support tenant-aware behaviour.

## Module Map
| Module | Purpose | Key Types |
| --- | --- | --- |
| Abstractions | Contracts for infrastructure capabilities (auth, persistence, messaging, storage, search, webhooks, secrets). | ITokenVerifier, IIdentityProvider, IDatabaseConnectionFactory, IMessagePublisher, IFileStorage, IWebhookDispatcher, etc. |
| Auth/Auth0 | Auth0 integration for token verification, OAuth flows, management API and webhook signature checks. | Auth0TokenVerifier, Auth0IdentityProvider, Auth0ManagementTokenProvider, Auth0UserDirectory, Auth0WebhookVerifier. |
| Persistence | Database access and outbox plumbing. | PostgresConnectionFactory, NoOpDatabaseMigrator, PostgresOutboxDispatcher. |
| Messaging | RabbitMQ-based messaging adapters. | RabbitMqConnectionProvider, RabbitMqMessagePublisher, RabbitMqMessageConsumer. |
| Caching | Distributed cache backed by Redis. | RedisDistributedCache, RedisOptions. |
| Storage | Object storage abstractions (in-memory default). | InMemoryFileStorage. |
| Search | Full-text indexing abstraction (no-op default). | NoOpSearchIndexer. |
| Notifications | Email/SMS/In-app dispatch abstraction. | NoOpNotificationSender. |
| Secrets | Secret management abstraction. | InMemorySecretStore. |
| Security | Signing/HMAC support. | HmacSigningService. |
| Webhooks | Outbound webhook delivery with retries and signing. | HttpWebhookDispatcher. |
| Extensions | Composition root utilities. | Auth0ServiceCollectionExtensions, InfrastructureServiceCollectionExtensions. |
| Support | Shared error helpers. | InfrastructureErrors. |

## Data & Control Flow
1. **Configuration**: AddInfrastructure binds configuration sections (Infrastructure:* and Auth0) to options, validates them, and registers concrete adapters (Redis cache, RabbitMQ messaging, etc.).
2. **Requests**: Interface layer (e.g., HTTP middleware) resolves ITokenVerifier to validate Auth0-issued JWTs. On success it receives an AuthenticatedPrincipal containing tenant, roles, scopes, and custom claims.
3. **Identity**: During login flows, the application calls IIdentityProvider.ExchangeCodeAsync to retrieve tokens. Auth0IdentityProvider exchanges codes/refresh tokens and returns typed results with expiry metadata. IUserDirectory (Auth0UserDirectory) ensures Auth0 app metadata mirrors tenant assignments.
4. **Persistence**: Application layer resolves IDatabaseConnectionFactory to open tenant-scoped connections (currently PostgreSQL). Outbox dispatch uses IOutboxDispatcher to deliver domain events.
5. **Messaging**: Domain events or integration messages are published through RabbitMQ (IMessagePublisher/IMessageConsumer), providing durable fan-out via topic exchanges.
6. **Caching**: IDistributedCache leverages Redis to store per-tenant payloads with absolute/sliding expiration.
7. **Storage**: File uploads and downloads flow through IFileStorage. Default implementation is in-memory for development; abstraction supports S3/Azure/Blob providers.
8. **Webhooks & Notifications**: Outbound webhooks are sent via IWebhookDispatcher, which signs payloads using ISigningService and retries with exponential backoff. Notifications use INotificationSender.
9. **Secrets & Signing**: Secrets are fetched through ISecretStore, enabling rotation without leaking provider details. HmacSigningService consumes secrets to sign payloads (webhooks, callbacks).
10. **Observability**: Infrastructure adapters emit structured logs (via ILogger), use correlation IDs, and rely on Yina.Observability for OpenTelemetry integration. Metrics/tags can be added in adapters as needed.

## Dependency Rules
- Domain (YinaCRM.Core) has no reference to infrastructure.
- Application (future) references YinaCRM.Infrastructure only via abstractions published in Abstractions/* namespace.
- Infrastructure references Yina.Common for Result, Error, IDs, retry helpers.
- Tests reference infrastructure project to exercise adapters and contract tests (with Testcontainers for Redis/RabbitMQ).

## Extensibility Strategy
- Swap providers by adding new implementations (e.g., Azure Service Bus, AWS SQS) and registering them instead of the RabbitMQ/Redis defaults.
- Auth0 module designed around interfaces (ITokenVerifier, IIdentityProvider, IAuth0ManagementTokenSource) to support alternative IdPs in future.
- Additional modules (jobs, scheduling, feature flags) can follow same pattern: define abstractions, implement provider, register in AddInfrastructure.

## Multi-Tenancy & Safety
- AuthenticatedPrincipal carries tenant id; adapters accept tenant in requests (CacheEntry.TenantId, FileUploadRequest.TenantId, etc.).
- Outbound operations enforce idempotency and retries; RabbitMQ publisher uses persistent delivery; Auth0 adapters log/key metrics for monitoring.
- Secrets never leave infrastructure layer; only derived signatures or temporary tokens cross boundaries.

## Deployment Topologies
- Works in single container or multi-service deployment. Options allow binding to environment variables, configuration files, or secret stores.
- Infrastructure module is library-only; runtime hosts (web API, workers) call AddInfrastructure to compose dependencies.\r\n\r\n## Sequence Diagram\r\n\r\n`mermaid\r\nsequenceDiagram\r\n    participant Domain\r\n    participant Outbox as Postgres Outbox\r\n    participant Dispatcher as Outbox Dispatcher\r\n    participant RabbitMQ\r\n    participant Consumer\r\n\r\n    Domain->>Outbox: Persist event row\r\n    note over Outbox: status=pending, attempts=0\r\n    Dispatcher->>Outbox: SELECT ... FOR UPDATE SKIP LOCKED\r\n    Outbox-->>Dispatcher: Pending events\r\n    Dispatcher->>RabbitMQ: Publish message\r\n    RabbitMQ-->>Dispatcher: Confirm (ACK)\r\n    Dispatcher->>Outbox: Update dispatched_at, attempts++\r\n    RabbitMQ-->>Consumer: Deliver message\r\n    Consumer->>RabbitMQ: ACK\r\n`\r\n\r\n`mermaid\r\nflowchart LR\r\n    subgraph RedisCacheKey[Redis Cache Key]\r\n    prefix[Key Prefix]\r\n    tenant[Tenant Id]\r\n    logical[Logical Key]\r\n    end\r\n\r\n    prefix --> combined((Combined))\r\n    tenant --> combined\r\n    logical --> combined\r\n    combined -->|Lowercase| finalKey[Redis Key]\r\n`\r\n