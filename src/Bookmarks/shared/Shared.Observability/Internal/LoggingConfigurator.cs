using OpenTelemetry.Logs;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>Wires the log pipeline from the consumer's options.</summary>
internal static class LoggingConfigurator
{
    public static void Configure(LoggerProviderBuilder logging, ObservabilityOptions options)
    {
        if (options.Baggage.AllowedKeyPrefixes.Count > 0)
        {
            logging.AddProcessor(new BaggageLogProcessor(options.Baggage));
        }

        if (options.Export.Console) logging.AddConsoleExporter();
        if (options.Export.Otlp is true)
            logging.AddOtlpExporter(otlp => OtlpExporterConfigurator.Apply(otlp, options));
    }

    /// <summary>
    /// Without these three the exported record carries the unrendered message template and
    /// drops every logging scope, which is what a log backend needs most.
    /// </summary>
    public static void ConfigureLoggerOptions(OpenTelemetryLoggerOptions loggerOptions)
    {
        loggerOptions.IncludeFormattedMessage = true;
        loggerOptions.IncludeScopes = true;
        loggerOptions.ParseStateValues = true;
    }
}
