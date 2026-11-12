using OpenTelemetry.Exporter;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>
/// Applies only the OTLP settings the consumer stated explicitly. Anything left unset is
/// not written, so the SDK keeps reading the standard <c>OTEL_EXPORTER_OTLP_*</c> variables.
/// </summary>
internal static class OtlpExporterConfigurator
{
    public const string EndpointConfigurationKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static void Apply(OtlpExporterOptions otlp, ObservabilityOptions options)
    {
        if (options.Export.OtlpEndpoint is { Length: > 0 } endpoint)
        {
            otlp.Endpoint = new Uri(endpoint);
        }

        if (options.Export.OtlpProtocol is { } protocol)
        {
            otlp.Protocol = protocol;
        }
    }
}
