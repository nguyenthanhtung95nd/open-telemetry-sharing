using OpenTelemetry.Metrics;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>Wires the metric pipeline from the consumer's options.</summary>
internal static class MetricsConfigurator
{
    private const string HostingMeter = "Microsoft.AspNetCore.Hosting";
    private const string KestrelMeter = "Microsoft.AspNetCore.Server.Kestrel";

    public static void Configure(MeterProviderBuilder metrics, ObservabilityOptions options)
    {
        AddInstrumentation(metrics, options.Instrumentation);

        foreach (var meter in options.Meters)
        {
            metrics.AddMeter(meter);
        }

        AddExporters(metrics, options);
    }

    private static void AddInstrumentation(
        MeterProviderBuilder metrics, ObservabilityInstrumentationOptions instrumentation)
    {
        if (instrumentation.AspNetCore)
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddMeter(HostingMeter)
                .AddMeter(KestrelMeter);
        }

        if (instrumentation.HttpClient) metrics.AddHttpClientInstrumentation();
        if (instrumentation.Runtime) metrics.AddRuntimeInstrumentation();
    }

    private static void AddExporters(MeterProviderBuilder metrics, ObservabilityOptions options)
    {
        if (options.Export.Console) metrics.AddConsoleExporter();
        if (options.Export.Otlp is true)
            metrics.AddOtlpExporter(otlp => OtlpExporterConfigurator.Apply(otlp, options));
    }
}
