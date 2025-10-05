# Environment Setup Guide

## 1. Configuration Conventions
- All infrastructure configuration lives under two sections:
  - `Auth0`: identity provider settings.
  - `Infrastructure`: nested sections for `Postgres`, `Redis`, `RabbitMq`, and `Outbox` (plus future modules).
- Prefer user-secrets or environment variables in local dev, and managed secret stores (Azure Key Vault, AWS Secrets Manager, etc.) in higher environments.
- Every setting is strongly typed and validated on startup. Missing/invalid values surface as startup failures.

## 2. Local Development
1. **Required tools**: .NET 8 SDK, Docker Desktop (for Postgres, Redis, RabbitMQ).
2. **Configuration** (`appsettings.Development.json` excerpt):
   ```json
   {
     "Auth0": {
       "Domain": "https://<tenant>.auth0.com",
       "ClientId": "dev-client-id",
       "ClientSecret": "dev-client-secret",
       "Audience": "https://api.dev.yinacrm.local",
       "AdditionalAudiences": ["https://graph.microsoft.com"],
       "Management": {
         "Audience": "https://<tenant>.auth0.com/api/v2/",
         "ClientId": "dev-mgmt-client",
         "ClientSecret": "dev-mgmt-secret"
       }
     },
     "Infrastructure": {
       "Postgres": {
         "ConnectionString": "Host=localhost;Port=5432;Database=yinacrm_dev;Username=postgres;Password=postgres;Pooling=true"
       },
       "Redis": {
         "ConnectionString": "localhost:6379",
         "KeyPrefix": "yinacrm-dev"
       },
       "RabbitMq": {
         "HostName": "localhost",
         "Port": 5672,
         "UserName": "guest",
         "Password": "guest",
         "ExchangeName": "yinacrm.events",
         "QueueName": "yinacrm.events.queue",
         "BindingKey": "crm.events.#"
       },
       "Outbox": {
         "TableName": "outbox_messages",
         "BatchSize": 50,
         "MaxAttempts": 10
       }
     }
   }
   ```
3. **Docker Compose** example:
   ```yaml
   services:
     postgres:
       image: postgres:16
       environment:
         POSTGRES_PASSWORD: postgres
         POSTGRES_USER: postgres
         POSTGRES_DB: yinacrm_dev
       ports: ["5432:5432"]
     redis:
       image: redis:7
       command: ["redis-server", "--appendonly", "yes"]
       ports: ["6379:6379"]
     rabbitmq:
       image: rabbitmq:3.13-management
       ports:
         - "5672:5672"
         - "15672:15672"
   ```
4. **Secrets**: for local hacking, `InMemorySecretStore` is used; secrets can be preloaded via configuration or tests.
5. **Running**: host application should call `services.AddInfrastructure(configuration)` from its composition root.
6. **Testing**: run `dotnet test`. Infrastructure tests spin Redis/RabbitMQ/Postgres containers automatically (requires Docker).

## 3. Development/Staging (Shared)
- Provision dedicated Auth0 applications per environment. Set `Auth0Options` via secret store / environment variables.
- Use managed PostgreSQL (Azure Flexible Server, AWS RDS), managed Redis (Azure Cache for Redis, AWS ElastiCache), and RabbitMQ (CloudAMQP, Amazon MQ, etc.).
- Enable TLS for Redis and RabbitMQ; update options accordingly (`UseSsl = true`, TLS ports).
- Configure RabbitMQ exchange/queue policies (durable topic exchange, DLQ, mirrored/quorum queues).
- Run an outbox background worker (hosted service or scheduled job) that invokes `IOutboxDispatcher.DispatchPendingAsync` on an interval.
- Rotate secrets regularly (Auth0 client secrets, Redis/RabbitMQ credentials) via `ISecretStore`.
- Ensure observability endpoints (OTLP collector) reachable; set `YinaObservabilityOptions` from configuration.

## 4. Production
- All secrets must live in a managed vault. Inject configuration via environment-specific secret references.
- Run database migrations before rolling out new binaries.
- Redis: enable persistence (AOF/RDB) and replication. Monitor keyspace and memory thresholds; configure eviction policy (`allkeys-lru` for cache workloads).
- RabbitMQ: enforce TLS, configure user-specific vhosts, set publisher confirms and dead-letter queues. Scale consumers with dedicated worker processes.
- Outbox: schedule cleanup job for dispatched rows (`CleanupDispatchedAfter`). Monitor `last_error` and `attempts` columns for stuck messages.
- Backup & DR: rely on managed backups for Postgres/Redis. Export RabbitMQ policies/definitions. Document outbox replay procedure.

## 5. CI/CD Integration
- Pipeline steps:
  1. Restore & build (`dotnet build`).
  2. Run tests (`dotnet test`).
  3. Optional: run static analysis / formatting.
  4. Publish artefacts with environment-specific configuration injected at deploy time.
- Infrastructure provisioning (Terraform/Pulumi) should output secrets/connection strings compatible with the options above.
- Smoke tests: publish a dummy event to outbox, run dispatcher once, assert message consumed.

## 6. Environment Variables Cheatsheet
| Setting | Description |
| --- | --- |
| `Auth0__Domain` | Auth0 tenant domain (https URL). |
| `Auth0__ClientId` / `Auth0__ClientSecret` | SPA/API client credentials for code exchange. |
| `Auth0__Audience` | API audience expected in JWT tokens. |
| `Auth0__Management__ClientId` / `ClientSecret` | Client credentials for management API. |
| `Infrastructure__Postgres__ConnectionString` | PostgreSQL connection string. |
| `Infrastructure__Redis__ConnectionString` | Redis connection string (include password if required). |
| `Infrastructure__Redis__KeyPrefix` | Prefix to namespace cache keys per environment. |
| `Infrastructure__RabbitMq__HostName` / `Port` | RabbitMQ host/port (5671 for TLS). |
| `Infrastructure__RabbitMq__ExchangeName` | Topic exchange used by publisher. |
| `Infrastructure__RabbitMq__QueueName` | Queue bound to exchange for consumers. |
| `Infrastructure__RabbitMq__BindingKey` | Routing key pattern (e.g., `crm.events.#`). |
| `Infrastructure__RabbitMq__EnablePublisherConfirms` | Toggle RabbitMQ publisher confirms (default `true`). |
| `Infrastructure__RabbitMq__MandatoryPublish` | Enforce mandatory publish / return handling. |
| `Infrastructure__Outbox__TableName` | Postgres table used for outbox storage. |
| `Infrastructure__Outbox__BatchSize` | Number of rows processed per dispatch iteration. |
| `Infrastructure__Outbox__MaxAttempts` | Max publish attempts before the dispatcher stops retrying. |

## 7. Verification Checklist
- [ ] `dotnet test` succeeds (Redis/RabbitMQ/Postgres Testcontainers pass).
- [ ] Auth0 JWKS reachable; token verification passes using `ITokenVerifier`.
- [ ] Database connection opens and migrations executed.
- [ ] Redis reachable; cache read/write smoke test passes.
- [ ] RabbitMQ reachable; publish/consume smoke test passes.
- [ ] Outbox dispatcher drains pending rows and marks them as dispatched.
- [ ] Webhook dispatcher can reach downstream endpoints.
- [ ] Secrets accessible via configured secret store.
- [ ] Observability pipeline (OTLP) receives traces/metrics/logs.
