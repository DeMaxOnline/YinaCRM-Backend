using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yina.Common.Diagnostics;
using Yina.Observability;
using Yina.Observability.Diagnostics;

namespace Yina.Observability.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddYinaObservability_ConfiguresTracingAndResource()
    {
        var spans = new List<Activity>();
        var services = new ServiceCollection();

        services.AddYinaObservability(options =>
        {
            options.ServiceName = "observability.tests";
            options.Environment = "test";
            options.UseOtlpExporter = false;
            options.EnableDefaultPropagators = false;
            options.ConfigureTracingBuilder = builder => builder.AddInMemoryExporter(spans);
        });

        using var provider = services.BuildServiceProvider();
        using var tracerProvider = provider.GetRequiredService<TracerProvider>();

        Correlation.SetCorrelationId(null);
        Correlation.SetCausationId(null);
        var expectedCorrelationId = Correlation.EnsureCorrelationId();

        using (var activity = ActivityConventions.Start("unit-test"))
        {
            activity?.SetTag(ActivityConventions.Keys.Success, true);
        }

        tracerProvider.ForceFlush();

        var span = Assert.Single(spans);
        Assert.Contains(span.Tags, tag => tag.Key == ActivityConventions.Keys.CorrelationId && tag.Value == expectedCorrelationId);
    }

    [Fact]
    public void AddYinaObservability_ExportsMetricsFromCommonMeter()
    {
        var metrics = new List<Metric>();
        var services = new ServiceCollection();

        services.AddYinaObservability(options =>
        {
            options.ServiceName = "metrics-tests";
            options.Environment = "test";
            options.UseOtlpExporter = false;
            options.EnableDefaultPropagators = false;
            options.ConfigureMetricsBuilder = builder => builder.AddInMemoryExporter(metrics);
        });

        using var provider = services.BuildServiceProvider();
        using var meterProvider = provider.GetRequiredService<MeterProvider>();

        var counter = MeterConventions.CommonMeter.CreateCounter<long>("observability.tests.counter");
        counter.Add(1);

        meterProvider.ForceFlush();

        Assert.Contains(metrics, metric => metric.Name == "observability.tests.counter");
    }

    [Fact]
    public void AddYinaObservability_ExportsLogsWithTraceContext()
    {
        var logRecords = new List<LogRecord>();
        var services = new ServiceCollection();

        services.AddYinaObservability(options =>
        {
            options.ServiceName = "log-tests";
            options.Environment = "test";
            options.UseOtlpExporter = false;
            options.EnableDefaultPropagators = false;
            options.ConfigureLoggingOptions = loggingOptions => loggingOptions.AddInMemoryExporter(logRecords);
        });

        using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("observability.tests");

        using (var activity = ActivityConventions.Start("log-export"))
        {
            logger.LogInformation("logging with trace context");
        }

        foreach (var loggerProvider in provider.GetServices<LoggerProvider>())
        {
            loggerProvider.ForceFlush();
        }

        Assert.Single(logRecords);
    }

    [Fact]
    public void AddYinaObservability_BindsConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["Observability:ServiceName"] = "config-service",
            ["Observability:Environment"] = "staging",
            ["Observability:EnableJsonConsoleLogger"] = "true",
            ["Observability:UseOtlpExporter"] = "false",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddYinaObservability(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<YinaObservabilityOptions>>().Value;

        Assert.Equal("config-service", options.ServiceName);
        Assert.Equal("staging", options.Environment);
        Assert.True(options.EnableJsonConsoleLogger);
        Assert.False(options.UseOtlpExporter);
    }

    [Fact]
    public void ConfigureAspNetCoreInstrumentation_AppliesCustomFilter()
    {
        var options = new YinaObservabilityOptions
        {
            AspNetCoreFilter = request => !request.Path.StartsWithSegments("/health"),
        };

        var instrumentationOptions = new AspNetCoreTraceInstrumentationOptions();
        var method = typeof(ServiceCollectionExtensions)
            .GetMethod("ConfigureAspNetCoreInstrumentation", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        method!.Invoke(null, new object[] { instrumentationOptions, options });

        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        Assert.NotNull(instrumentationOptions.Filter);
        Assert.False(instrumentationOptions.Filter!(context));
    }

    [Fact]
    public void ConfigureHttpClientInstrumentation_AppliesCustomFilter()
    {
        var options = new YinaObservabilityOptions
        {
            HttpClientFilter = request => request.RequestUri?.Host != "localhost",
        };

        var instrumentationOptions = new HttpClientTraceInstrumentationOptions();
        var method = typeof(ServiceCollectionExtensions)
            .GetMethod("ConfigureHttpClientInstrumentation", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        method!.Invoke(null, new object[] { instrumentationOptions, options });

        var blocked = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api");
        var allowed = new HttpRequestMessage(HttpMethod.Get, "https://api.internal/api");

        Assert.NotNull(instrumentationOptions.FilterHttpRequestMessage);
        Assert.False(instrumentationOptions.FilterHttpRequestMessage!(blocked));
        Assert.True(instrumentationOptions.FilterHttpRequestMessage!(allowed));
    }
}
