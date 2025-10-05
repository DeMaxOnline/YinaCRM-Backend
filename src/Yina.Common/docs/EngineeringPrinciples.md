# Engineering Principles (Inspired by Netflix)

- Freedom & Responsibility: teams own their services end-to-end; Core provides guardrails (strong types, VOs, Result/Errors) and a paved path.
- Paved Path: opinionated defaults over squeeze-the-balloon customization. We optimize for maintainable velocity, not one-off hacks.
- Context, not Control: codify domain decisions (CoreRules, ADRs) and reduce tribal knowledge via docs and templates.
- Operability by Design: UTC timestamps, idempotent operations, event versioning, and clear error codes enable reliable operations.
- Resilience & Evolution: isolate Core from infrastructure, use outbox for delivery guarantees, adopt specifications for query evolution.
- Observability: emit structured events with timestamps and versions; outer layers correlate with traces/metrics/logs.
- Security & Compliance: no secrets in Core; Secret VO masks values. AuthZ lives outside Core.

## Quality Bar
- Tests: coverage gate ≥ 95% line for Core; meaningful specs, not just counters.
- APIs: composable, explicit, typed; no exceptions for control flow; no raw strings/guids across boundaries.
- Time/Identity: always UTC; StrongId for identifiers; VO normalization.

