# Yina.Common

Shared primitives and infrastructure helpers for backend services.

## Packages
- **Abstractions/Errors** – typed error representation (`Error`) and helper factories (`Errors.*`).
- **Abstractions/Results** – `Result` / `Result<T>` with extension pipelines for flow, error handling, and side-effects.
- **Foundation** – strong IDs, pagination, clock abstractions, guard helpers.
- **Protocols** – lightweight messaging contracts (`ICommand`, `IQuery`, `IEvent`, envelopes/metadata).
- **Serialization** – JSON defaults and converters (e.g., `StrongId` support, `DateOnly`, `TimeOnly`).
- **Resilience** – retry executor, classifiers, and jittered backoff strategies.
- **Caching** – in-memory cache abstraction with configurable eviction.
  - For size-based eviction, specify `CacheEntryOptions.Size` when storing entries so capacity limits apply.
- **Diagnostics** – activity source conventions and correlation helpers.
- **Validation** – `ValidationResult` / `ValidationError` for aggregating failures.

## Usage
```csharp
// Results + Errors
var result = Result.Success()
    .Then(() => Validate(input))
    .Then(() => Process(input))
    .Compensate(error => Recover(error));

if (result.IsFailure)
{
    logger.LogWarning("Processing failed: {Error}", result.Error.Code);
}

// StrongId serialization
var options = JsonDefaults.Create();
var payload = JsonSerializer.Serialize(clientId, options); // handles StrongId<T>

// Retry executor
await RetryExecutor.ExecuteAsync(
    action: ct => SendAsync(ct),
    options: new RetryOptions { MaxAttempts = 5 },
    onRetry: (attempt, ex) => logger.LogWarning(ex, "Retrying attempt {Attempt}", attempt));

// Cache
var cache = new InMemoryCache();
var value = await cache.GetOrAddAsync(
    key: "clients:123",
    factory: _ => FetchClientAsync(),
    options: new CacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
```

## Notes
- Exceptions are sanitized by default (`Errors.FromException`) to avoid leaking internal details.
- `Result` defaults (struct default) evaluate to `IsSuccess == true` with `Error.None`.
- Activity source name: `yina.common` (consume via OpenTelemetry).

\n## Thread-Safety & Performance\n- InMemoryCache is thread-safe; capacity enforcement is best-effort and requires CacheEntryOptions.Size.\n- JitteredExponentialBackoffStrategy uses Random.Shared and is safe to share across threads.\n

## Installation
 ```bash 
dotnet add package Yina.Common
 ``` 
