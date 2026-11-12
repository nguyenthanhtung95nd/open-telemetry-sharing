using OpenTelemetry.Exporter;

namespace Shared.Observability.Options;

/// <summary>
/// Where the signals go. Anything left unset is deliberately not written to the SDK, so the
/// standard <c>OTEL_EXPORTER_OTLP_*</c> environment variables keep working.
/// </summary>
public sealed class ObservabilityExportOptions
{
    /// <summary>
    /// Write traces, metrics and logs to stdout. Off by default because it duplicates every
    /// signal already going to the collector and is synchronous on the hot path.
    /// </summary>
    public bool Console { get; set; }

    /// <summary>
    /// Export over OTLP. Left unset it means "on when an endpoint is configured", which
    /// avoids a service with no collector retrying a connection it will never get.
    /// </summary>
    public bool? Otlp { get; set; }

    /// <summary>OTLP endpoint. Unset defers to <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>.</summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>OTLP protocol. Unset defers to <c>OTEL_EXPORTER_OTLP_PROTOCOL</c>.</summary>
    public OtlpExportProtocol? OtlpProtocol { get; set; }
}
