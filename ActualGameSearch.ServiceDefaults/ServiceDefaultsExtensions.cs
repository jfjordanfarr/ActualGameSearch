using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

namespace ActualGameSearch.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddServiceObservability(this IServiceCollection services, string serviceName)
    {
        var otlp = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: serviceName))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation()
                 .AddSource("ActualGameSearch.Api")
                 .AddSource("ActualGameSearch.Steam");
                if (!string.IsNullOrWhiteSpace(otlp))
                {
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
                }
                // Fallback: rely on normal console logging if OTLP not configured.
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation()
                 .AddRuntimeInstrumentation()
                 .AddMeter("ActualGameSearch.Core");
                if (!string.IsNullOrWhiteSpace(otlp))
                {
                    m.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
                }
                // Fallback: metrics not exported if no OTLP endpoint (can add console exporter package later).
            })
            .WithLogging(l =>
            {
                if (!string.IsNullOrWhiteSpace(otlp))
                {
                    l.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
                }
                // Fallback: logs go through standard logger providers only.
            });
        return services;
    }
}
