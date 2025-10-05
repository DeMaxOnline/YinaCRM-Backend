using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Yina.Common.Diagnostics;
using Yina.Observability.Diagnostics;

namespace Yina.Observability;

/// <summary>Options controlling how Yina services configure OpenTelemetry.</summary>
public sealed class YinaObservabilityOptions
{
    public YinaObservabilityOptions()
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        ServiceName = entryAssembly?.GetName().Name ?? "unknown-service";
        ServiceNamespace = entryAssembly?.GetName().Name;
        ServiceVersion = entryAssembly?.GetName().Version?.ToString();
        ServiceInstanceId = System.Environment.MachineName;
        UseOtlpExporter = !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
    }

    public string ServiceName { get; set; }

    public string? ServiceNamespace { get; set; }

    public string? ServiceVersion { get; set; }

    public string Environment { get; set; } = "development";

    public string? ServiceInstanceId { get; set; }

    public bool EnableTracing { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public bool EnableLogging { get; set; } = true;

    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    public bool EnableHttpClientInstrumentation { get; set; } = true;

    public bool EnableRuntimeInstrumentation { get; set; } = true;

    public bool EnableConsoleTraceExporter { get; set; }

    public bool EnableConsoleMetricExporter { get; set; }

    public bool EnableConsoleLogExporter { get; set; }

    public bool EnableJsonConsoleLogger { get; set; }

    public bool IncludeFormattedMessage { get; set; } = true;

    public bool IncludeScopes { get; set; } = true;

    public bool ParseStateValues { get; set; } = true;

    public bool EnableDefaultPropagators { get; set; } = true;

    public double TraceSamplerProbability { get; set; } = 1d;

    public Sampler? CustomSampler { get; set; }

    public Uri? OtlpEndpoint { get; set; }

    public OtlpExportProtocol OtlpProtocol { get; set; } = OtlpExportProtocol.Grpc;

    public bool UseOtlpExporter { get; set; }

    public Action<OtlpExporterOptions>? ConfigureOtlpExporter { get; set; }

    public Func<HttpRequest, bool>? AspNetCoreFilter { get; set; }

    public Func<HttpRequestMessage, bool>? HttpClientFilter { get; set; }

    public ISet<string> ActivitySources { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ActivityConventions.SourceName,
    };

    public ISet<string> Meters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        MeterConventions.CommonMeterName,
    };

    public IDictionary<string, object> ResourceAttributes { get; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public Action<TracerProviderBuilder>? ConfigureTracingBuilder { get; set; }

    public Action<MeterProviderBuilder>? ConfigureMetricsBuilder { get; set; }

    public Action<OpenTelemetryLoggerOptions>? ConfigureLoggingOptions { get; set; }
}

