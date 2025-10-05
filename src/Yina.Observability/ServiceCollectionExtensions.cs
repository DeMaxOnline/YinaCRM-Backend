using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yina.Common.Diagnostics;
using Yina.Observability.Diagnostics;

namespace Yina.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYinaObservability(
        this IServiceCollection services,
        Action<YinaObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<YinaObservabilityOptions>()
            .Configure(opt =>
            {
                configure?.Invoke(opt);
            })
            .PostConfigure(opt => Validate(opt));

        var options = new YinaObservabilityOptions();
        configure?.Invoke(options);
        Validate(options);

        if (options.EnableDefaultPropagators)
        {
            Sdk.SetDefaultTextMapPropagator(
                new CompositeTextMapPropagator(
                    new TextMapPropagator[]
                    {
                        new TraceContextPropagator(),
                        new BaggagePropagator(),
                    }));
        }

        if (options.EnableTracing || options.EnableMetrics)
        {
            var otelBuilder = services.AddOpenTelemetry();
            otelBuilder.ConfigureResource(resource => ConfigureResource(resource, options));

            if (options.EnableTracing)
            {
                otelBuilder.WithTracing(tracing => ConfigureTracing(tracing, options));
            }

            if (options.EnableMetrics)
            {
                otelBuilder.WithMetrics(metrics => ConfigureMetrics(metrics, options));
            }
        }

        if (options.EnableLogging)
        {
            services.AddLogging(logging =>
            {
                if (options.EnableJsonConsoleLogger)
                {
                    logging.AddConsole(o => o.FormatterName = ConsoleFormatterNames.Json);
                }

                logging.AddOpenTelemetry(loggingOptions => ConfigureLogging(loggingOptions, options));
            });
        }

        return services;
    }

    public static IServiceCollection AddYinaObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Observability")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddYinaObservability(options => configuration.GetSection(sectionName).Bind(options));
    }

    private static void ConfigureResource(ResourceBuilder resourceBuilder, YinaObservabilityOptions options)
    {
        var serviceInstanceId = string.IsNullOrWhiteSpace(options.ServiceInstanceId)
            ? System.Environment.MachineName
            : options.ServiceInstanceId;

        resourceBuilder.AddService(
            options.ServiceName,
            serviceNamespace: options.ServiceNamespace,
            serviceVersion: options.ServiceVersion,
            serviceInstanceId: serviceInstanceId);

        var attributes = new List<KeyValuePair<string, object>>
        {
            new("deployment.environment", options.Environment),
        };

        foreach (var attribute in options.ResourceAttributes)
        {
            attributes.Add(new(attribute.Key, attribute.Value));
        }

        resourceBuilder.AddAttributes(attributes);
    }

    private static void ConfigureTracing(TracerProviderBuilder builder, YinaObservabilityOptions options)
    {
        foreach (var source in options.ActivitySources)
        {
            builder.AddSource(source);
        }

        builder.SetSampler(CreateSampler(options));

        if (options.EnableAspNetCoreInstrumentation)
        {
            builder.AddAspNetCoreInstrumentation(o => ConfigureAspNetCoreInstrumentation(o, options));
        }

        if (options.EnableHttpClientInstrumentation)
        {
            builder.AddHttpClientInstrumentation(o => ConfigureHttpClientInstrumentation(o, options));
        }

        if (options.UseOtlpExporter)
        {
            builder.AddOtlpExporter(exporter => ApplyOtlpOptions(exporter, options));
        }

        if (options.EnableConsoleTraceExporter)
        {
            builder.AddConsoleExporter();
        }

        options.ConfigureTracingBuilder?.Invoke(builder);
    }

    private static void ConfigureMetrics(MeterProviderBuilder builder, YinaObservabilityOptions options)
    {
        foreach (var meter in options.Meters)
        {
            builder.AddMeter(meter);
        }

        if (options.EnableRuntimeInstrumentation)
        {
            builder.AddRuntimeInstrumentation();
        }

        if (options.UseOtlpExporter)
        {
            builder.AddOtlpExporter(exporter => ApplyOtlpOptions(exporter, options));
        }

        if (options.EnableConsoleMetricExporter)
        {
            builder.AddConsoleExporter();
        }

        options.ConfigureMetricsBuilder?.Invoke(builder);
    }

    private static void ConfigureLogging(OpenTelemetryLoggerOptions loggingOptions, YinaObservabilityOptions options)
    {
        loggingOptions.IncludeFormattedMessage = options.IncludeFormattedMessage;
        loggingOptions.ParseStateValues = options.ParseStateValues;
        loggingOptions.IncludeScopes = options.IncludeScopes;

        var resourceBuilder = ResourceBuilder.CreateDefault();
        ConfigureResource(resourceBuilder, options);
        loggingOptions.SetResourceBuilder(resourceBuilder);

        if (options.UseOtlpExporter)
        {
            loggingOptions.AddOtlpExporter(exporter => ApplyOtlpOptions(exporter, options));
        }

        if (options.EnableConsoleLogExporter)
        {
            loggingOptions.AddConsoleExporter();
        }

        options.ConfigureLoggingOptions?.Invoke(loggingOptions);
    }

    private static void ConfigureAspNetCoreInstrumentation(
        AspNetCoreTraceInstrumentationOptions instrumentationOptions,
        YinaObservabilityOptions options)
    {
        instrumentationOptions.RecordException = true;

        if (options.AspNetCoreFilter is not null)
        {
            instrumentationOptions.Filter = context => options.AspNetCoreFilter!(context.Request);
        }

        instrumentationOptions.EnrichWithHttpRequest = (activity, request) =>
        {
            var correlationId = EnsureCorrelationFromRequest(request);
            activity.SetTag(ActivityConventions.Keys.CorrelationId, correlationId);

            var causationId = activity.SpanId.ToString();
            Correlation.SetCausationId(causationId);
            activity.SetTag(ActivityConventions.Keys.CausationId, causationId);

            if (request.Headers.TryGetValue(HeaderNames.UserId, out var userHeader) &&
                !string.IsNullOrWhiteSpace(userHeader))
            {
                activity.SetTag(ActivityConventions.Keys.UserId, userHeader.ToString());
            }
        };

        instrumentationOptions.EnrichWithHttpResponse = (activity, response) =>
        {
            var correlationId = Correlation.GetCorrelationId();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                response.Headers[HeaderNames.CorrelationId] = correlationId;
            }
        };
    }

    private static void ConfigureHttpClientInstrumentation(
        HttpClientTraceInstrumentationOptions instrumentationOptions,
        YinaObservabilityOptions options)
    {
        instrumentationOptions.RecordException = true;

        if (options.HttpClientFilter is not null)
        {
            instrumentationOptions.FilterHttpRequestMessage = options.HttpClientFilter;
        }

        instrumentationOptions.EnrichWithHttpRequestMessage = (activity, request) =>
        {
            var correlationId = Correlation.EnsureCorrelationId();
            if (!request.Headers.Contains(HeaderNames.CorrelationId))
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.CorrelationId, correlationId);
            }

            var causationId = activity.SpanId.ToString();
            Correlation.SetCausationId(causationId);

            if (!request.Headers.Contains(HeaderNames.RequestId))
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.RequestId, causationId);
            }
        };

        instrumentationOptions.EnrichWithHttpResponseMessage = (activity, response) =>
        {
            activity.SetTag(ActivityConventions.Keys.Success, response.IsSuccessStatusCode);
        };
    }

    private static string EnsureCorrelationFromRequest(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderNames.CorrelationId, out var existing) &&
            !string.IsNullOrWhiteSpace(existing))
        {
            var value = existing.ToString();
            Correlation.SetCorrelationId(value);
            return value;
        }

        var generated = Correlation.EnsureCorrelationId();
        request.Headers[HeaderNames.CorrelationId] = generated;
        return generated;
    }

    private static void ApplyOtlpOptions(OtlpExporterOptions exporterOptions, YinaObservabilityOptions options)
    {
        exporterOptions.Protocol = options.OtlpProtocol;
        if (options.OtlpEndpoint is not null)
        {
            exporterOptions.Endpoint = options.OtlpEndpoint;
        }

        options.ConfigureOtlpExporter?.Invoke(exporterOptions);
    }

    private static Sampler CreateSampler(YinaObservabilityOptions options)
    {
        if (options.CustomSampler is not null)
        {
            return options.CustomSampler;
        }

        var probability = Math.Clamp(options.TraceSamplerProbability, 0d, 1d);

        if (probability <= 0d)
        {
            return new AlwaysOffSampler();
        }

        if (Math.Abs(probability - 1d) < 0.0001)
        {
            return new AlwaysOnSampler();
        }

        return new ParentBasedSampler(new TraceIdRatioBasedSampler(probability));
    }

    private static void Validate(YinaObservabilityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new ArgumentException("ServiceName must be provided.");
        }

        if (options.TraceSamplerProbability is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.TraceSamplerProbability),
                options.TraceSamplerProbability,
                "TraceSamplerProbability must be between 0.0 and 1.0 inclusive.");
        }
    }
}





