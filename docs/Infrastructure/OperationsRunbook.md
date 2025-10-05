# Infrastructure Operations Runbook

## 1. Monitoring & Alerting
- **Identity (Auth0)**: token validation failures, JWKS refresh errors, management token fetch latency.
- **Database (Postgres)**: connection pool utilisation, query latency, deadlocks.
- **Redis Cache**:
  - Metrics: keyspace hits/misses, memory usage, evictions, replication/backlog.
  - Alerts: connection failures, high memory fragmentation, latency spikes, replica lag.
- **RabbitMQ Messaging**:
  - Metrics: publish latency, queue depth, unacked messages, consumer utilisation.
  - Alerts: connection failures, DLQ growth, queue ready message spikes, channel closure.
- **Outbox Dispatcher**:
  - Metrics: records processed, queue depth (`SELECT COUNT(*) WHERE dispatched_at IS NULL`), average attempts, last dispatch runtime.
  - Alerts: `last_error` not null for sustained period, attempts >= `MaxAttempts`, dispatcher lag.
- **Webhooks**: delivery success rate, attempts per message, median latency.
- **Secrets/Signing**: secret fetch errors, signature verification failures.
- **Observability**: ensure OTLP exporter emits traces/metrics; define SLOs for API latency, 500 rate, webhook success rate.

## 2. Incident Response Playbooks
### Auth0 Token Verification Failing
1. Check recent deployments for configuration changes (`Auth0` section).
2. Confirm Auth0 tenant status page for outages.
3. Validate JWKS endpoint `/.well-known/jwks.json` accessible from environment.
4. Restart services to force configuration refresh if keys rotated unexpectedly.
5. If tokens issued with unexpected issuer/audience, investigate upstream application.

### Redis Issues
1. Inspect connection metrics and logs for `REDIS_CACHE_*` errors.
2. Check Redis monitoring (latency, `INFO` command) for blocked clients or memory pressure.
3. Validate network routes/security groups; ensure TLS cert valid if enabled.
4. For cluster/replica setups, failover or scale vertically/horizontally as needed.
5. If dataset corrupt, restore from latest snapshot/AOF and rebuild cache gradually.

### RabbitMQ Issues
1. Review logs for `RABBITMQ_*` errors; inspect management UI for queue depths and connection status.
2. Validate credentials, TLS certificates, and network connectivity.
3. Scale consumers if queues back up; adjust prefetch and DLQ policies.
4. For persistent failures, drain queue to DLQ and replay after remediation.

### Outbox Dispatcher Stuck
1. Query outbox table for `last_error` messages and high `attempts` values.
2. Check dispatcher logs for `OUTBOX_*` errors (publish / dispatch failures).
3. If message type cannot be resolved, deploy fix to include the assembly or adjust serialization.
4. For transient RabbitMQ/Redis outages, re-run dispatcher once dependencies recover.
5. For poison messages, manually inspect row, apply business decision (drop, patch payload), record in postmortem.

### Management API Errors (Provisioning, Metadata Update)
1. Inspect logs around `AUTH0_USER_FETCH_FAILED` / `AUTH0_USER_PATCH_FAILED`.
2. Validate management client credentials/scopes in Auth0 dashboard.
3. Check rate limits (`429`): throttle operations or request limit increase.
4. Temporarily disable automated metadata sync; plan catch-up job after fix.

### Database Connectivity Issues
1. Check connection string configuration and secret store values.
2. Validate network security group / firewall rules allowing host to reach database.
3. Failover to standby or read replica if master unavailable.
4. If migrations failed, rollback deployment and investigate migration scripts.

### Webhook Delivery Failures
1. Review logs (look for `WEBHOOK_DELIVERY_FAILED`, status codes, response bodies).
2. Confirm downstream endpoint availability and TLS cert validity.
3. Adjust retry policy / backoff for transient failures.
4. Use `ISecretStore` rotation to replace signing secret if consumer reports signature mismatch.
5. For mass failures, disable webhook dispatch or route to DLQ until issue resolved.

### Secret Rotation
1. Use `ISecretStore.RotateSecretAsync` with new value generator.
2. For Auth0 webhook secrets, update both Auth0 Hook and infrastructure secret store atomically.
3. For HMAC signing keys, rotate per tenant and propagate to consumers (communicate new key ids).

## 3. Backup & Restore
- **Database**: rely on managed service backups (point-in-time recovery). Document `pg_dump` or cloud-provider backup schedule.
- **Redis**: ensure persistence (AOF/RDB) enabled; snapshot before major releases.
- **RabbitMQ**: export definitions (exchanges, queues, bindings) and replicate across clusters; consider mirrored/quorum queues.
- **Outbox**: included in Postgres backups. For manual recovery, replay undeclared rows through dispatcher.
- **Secrets**: ensure secret store has versioning. Keep historical versions for rollbacks.
- **Configuration**: treat IaC and configuration as code in VCS.

## 4. Deployment Checklist
- [ ] Migrations applied successfully (no pending scripts).
- [ ] Auth0 credentials validated (test token exchange in staging).
- [ ] Redis reachable; cache read/write smoke test passes.
- [ ] RabbitMQ reachable; publish/consume smoke test passes.
- [ ] Outbox dispatcher run verified (no stuck rows).
- [ ] Observability endpoint reachable (smoke trace).
- [ ] Webhook test event delivered to downstream sample endpoint.
- [ ] Secrets rotated or validated before major release.

## 5. Disaster Recovery
1. **Auth0 outage**: enable maintenance mode (gracefully degrade to cached tokens if possible). Communicate to customers.
2. **Database unavailable**: failover to replica region (if configured). Restore from latest backup and redeploy infrastructure stack.
3. **Redis outage**: switch cache clients to replica/secondary, flush stale data, or fall back to in-memory caching temporarily.
4. **RabbitMQ outage**: failover to secondary cluster; replay events from outbox or DLQ once primary restored.
5. **Outbox corruption**: restore latest database backup, diff undelivered rows, replay events carefully (idempotent handlers required).
6. **Secret store downtime**: fall back to cached secrets (if safe) or pause operations until availability returns.
7. **Webhook provider outage**: disable dispatcher or reroute through alternative queue; reprocess DLQ after recovery.

## 6. On-Call Tips
- Keep `dotnet test` passing locally before shipping patches.
- Use `dotnet user-secrets` or environment overrides for emergency fixes.
- Familiarise with DI registration in `InfrastructureServiceCollectionExtensions`; most toggles can be swapped via service registration.
- Document incident timelines and postmortems in shared knowledge base.
