using OpenTelemetry.Trace;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>Wires the trace pipeline from the consumer's options.</summary>
internal static class TracingConfigurator
{
    public static void Configure(TracerProviderBuilder tracing, ObservabilityOptions options)
    {
        AddSampler(tracing, options);
        AddInstrumentation(tracing, options.Instrumentation);

        foreach (var source in options.ActivitySources)
        {
            tracing.AddSource(source);
        }

        if (options.Baggage.AllowedKeyPrefixes.Count > 0)
        {
            tracing.AddProcessor(new BaggageTagProcessor(options.Baggage));
        }

        AddExporters(tracing, options);
    }

    private static void AddSampler(TracerProviderBuilder tracing, ObservabilityOptions options)
    {
        var sampler = ObservabilitySamplerFactory.Create(options.Sampling);
        if (sampler is not null)
        {
            tracing.SetSampler(sampler);
        }
    }

    private static void AddInstrumentation(
        TracerProviderBuilder tracing, ObservabilityInstrumentationOptions instrumentation)
    {
        if (instrumentation.AspNetCore) tracing.AddAspNetCoreInstrumentation();
        if (instrumentation.HttpClient)
            tracing.AddHttpClientInstrumentation(http => http.RecordException = true);
    }

    private static void AddExporters(TracerProviderBuilder tracing, ObservabilityOptions options)
    {
        if (options.Export.Console) tracing.AddConsoleExporter();
        if (options.Export.Otlp is true)
            tracing.AddOtlpExporter(otlp => OtlpExporterConfigurator.Apply(otlp, options));
    }
}
