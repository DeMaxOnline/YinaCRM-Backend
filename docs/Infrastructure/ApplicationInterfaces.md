# Application Interface Reference

This guide explains how the application layer consumes the infrastructure abstractions exported by `YinaCRM.Infrastructure`.

## Authentication & Identity
- **`ITokenVerifier`**: verifies bearer tokens at request ingress.
  ```csharp
  var result = await tokenVerifier.VerifyAsync(token, cancellationToken);
  if (result.IsFailure) { /* return 401 */ }
  var principal = result.Value; // contains SubjectId, TenantId, Roles, Scopes
  ```
- **`IIdentityProvider`**: exchanges authorization codes and refresh tokens.
  ```csharp
  var exchange = await identityProvider.ExchangeCodeAsync(new CodeExchangeRequest(code, redirectUri, codeVerifier, tenantHint));
  var refresh = await identityProvider.RefreshAsync(new TokenRefreshRequest(refreshToken, tenantHint));
  await identityProvider.RevokeAsync(new TokenRevokeRequest(token, TokenRevokeType.RefreshToken, tenantHint));
  ```
- **`IUserDirectory`**: ensures external users mapped to internal records.
  ```csharp
  var sync = await userDirectory.EnsureUserAsync(principal);
  if (sync.IsFailure) { /* handle provisioning error */ }
  ```

## Persistence
- **`IDatabaseConnectionFactory`**: open tenant-aware connections.
  ```csharp
  await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
  // perform Dapper/EF operations
  ```
- **`IDatabaseMigrator`**: apply schema changes on startup or migration job.
  ```csharp
  await migrator.ApplyMigrationsAsync(ct);
  ```
- **`IOutboxDispatcher`**: flush domain events.
  ```csharp
  await outboxDispatcher.DispatchPendingAsync(ct);
  ```

## Messaging
- **`IMessagePublisher`**: publish integration events.
  ```csharp
  var envelope = new MessageEnvelope(message, topic, tenantId, headers);
  await publisher.PublishAsync(envelope, ct);
  ```
- **`IMessageConsumer`**: used by background workers to process message streams.
  ```csharp
  await consumer.StartAsync(async (incoming, ct) => { /* deserialize/process */ return Result.Success(); }, ct);
  ```

## Storage & Files
- **`IFileStorage`**: upload, download, delete tenant files.
  ```csharp
  await fileStorage.UploadAsync(new FileUploadRequest(tenantId, path, contentType, stream, metadata, ttl), ct);
  var download = await fileStorage.DownloadAsync(new FileDownloadRequest(tenantId, path, asSignedUrl: true, validFor: TimeSpan.FromMinutes(5)), ct);
  await fileStorage.DeleteAsync(new FileDeleteRequest(tenantId, path, hardDelete: false), ct);
  ```

## Caching
- **`IDistributedCache`**: store JSON blobs or binary payloads per tenant.
  ```csharp
  var entry = new CacheEntry(tenantId, key, payload, contentType, now, absoluteExpiration, slidingExpiration, tags);
  await cache.SetAsync(entry, ct);
  var cached = await cache.GetAsync(new CacheReadRequest(tenantId, key), ct);
  ```

## Search
- **`ISearchIndexer`**: index or remove documents from search backend.
  ```csharp
  await indexer.IndexAsync(new SearchDocument(index, id, tenantId, fields, boosts, updatedAtUtc), ct);
  await indexer.RemoveAsync(new SearchDocumentReference(index, id, tenantId), ct);
  ```

## Notifications
- **`INotificationSender`**: send email/SMS/voice/in-app notifications.
  ```csharp
  var message = new NotificationMessage(NotificationChannel.Email, recipient, templateId, tokens, tenantId, correlationId);
  await notificationSender.SendAsync(message, ct);
  ```

## Webhooks & Signing
- **`IWebhookDispatcher`**: deliver outbound webhook events.
  ```csharp
  var request = new WebhookDispatchRequest(endpoint, secret, tenantId, eventType, payload, headers, timeout, maxAttempts);
  await webhookDispatcher.DispatchAsync(request, ct);
  ```
- **`IWebhookSignatureVerifier`**: validate inbound webhook signatures (Auth0 actions, etc.).
  ```csharp
  var verify = webhookVerifier.VerifySignature(new WebhookVerificationContext(secret, signature, payload, algorithm, receivedAtUtc, tenantId));
  ```
- **`ISigningService`**: create/verify HMAC signatures for internal flows (idempotency, request signing).
  ```csharp
  var signResult = await signingService.SignAsync(new SignRequest(tenantId, payload, "sha256", keyId, headers), ct);
  var verifyResult = await signingService.VerifyAsync(new VerifySignatureRequest(tenantId, payload, signature, "sha256", keyId, headers), ct);
  ```

## Secrets
- **`ISecretStore`**: read and rotate secrets.
  ```csharp
  var secret = await secretStore.GetSecretAsync("auth0/webhook/tenant-1", ct);
  await secretStore.RotateSecretAsync("signing/tenant-1", GenerateNewSecret, ct);
  ```

## Observability & Configuration
- Use `Auth0ServiceCollectionExtensions.AddAuth0Infrastructure()` and `InfrastructureServiceCollectionExtensions.AddInfrastructure()` from startup to register all services.
- For bespoke HTTP clients, add named `HttpClient` entries (e.g., `webhooks`, `auth0`) to configure proxies, TLS, and resilience policies.

## Testing Guidance
- Infrastructure tests demonstrate how to stub HTTP handlers (`TestHttpMessageHandler`) and options (`StubOptionsMonitor`).
- Application tests can replace adapters with fakes by registering custom implementations of the abstractions in DI.\r\n\r\n## Outbox
- **IOutboxDispatcher**: drains the Postgres outbox table and republishes messages.
  `csharp
  // Run from a hosted service / background worker
  while (!stoppingToken.IsCancellationRequested)
  {
      var result = await outboxDispatcher.DispatchPendingAsync(stoppingToken);
      if (result.IsFailure)
      {
          logger.LogError("Outbox dispatch failed: {Error}", result.Error.Message);
      }

      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
  }
  `
- Outbox rows must include message_type (assembly qualified) and JSON payload. Dispatcher deserialises and republishes through IMessagePublisher.
