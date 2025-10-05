# Yina.Observability

Opinionated OpenTelemetry setup for Yina services. Reuses `Yina.Common.Diagnostics` correlation helpers and exposes a single `AddYinaObservability` entry point for applications.

## Highlights
- Configures traces, metrics, and logs with a shared resource builder.
- Registers default ActivitySource (`yinacrm.common`) and meter (`yinacrm.common`).
- Adds ASP.NET Core and HTTP client instrumentation with correlation propagation and optional filters.
- Keeps correlation flowing across HTTP boundaries and can emit JSON console logs for Promtail/Loki pipelines.
- OTLP exporters are disabled by default; enable explicitly or provide `OTEL_EXPORTER_OTLP_ENDPOINT`.
- Supports OTLP and console exporters, plus optional customization callbacks and default W3C propagators.

## Usage
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddYinaObservability(options =>
{
    options.Environment = builder.Environment.EnvironmentName;
    options.OtlpEndpoint = new Uri("http://collector:4317");
    options.ServiceName = "yina.api";
    options.EnableJsonConsoleLogger = true; // optional structured stdout for Loki
    options.AspNetCoreFilter = request => !request.Path.StartsWithSegments("/health");
});
```

Configuration can also be bound from `IConfiguration` via:

```csharp
builder.Services.AddYinaObservability(builder.Configuration);
```

See `YinaObservabilityOptions` for all knobs.

### OTLP Export
`UseOtlpExporter` defaults to false so local development does not try to reach a collector.
Explicitly enable it or rely on environment variables:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317
export Observability__UseOtlpExporter=true
```

When `OTEL_EXPORTER_OTLP_ENDPOINT` is set the option is enabled automatically.

### Correlation in Logs
Wrap logging scopes when you want correlation identifiers in JSON console output:

```csharp
using var scope = logger.BeginScope(new Dictionary<string, object?>
{
    [ActivityConventions.Keys.CorrelationId] = Correlation.EnsureCorrelationId(),
});

logger.LogInformation("processed request");
```
