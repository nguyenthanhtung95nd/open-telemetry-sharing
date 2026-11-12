namespace Shared.Observability.Options;

/// <summary>
/// Everything a consuming application tells the shared observability library about itself.
/// Bound from the <see cref="SectionName"/> configuration section and validated before the
/// OpenTelemetry providers are constructed.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>Configuration section this options object is bound from.</summary>
    public const string SectionName = "Observability";

    /// <summary>Logical service name, written to <c>service.name</c>. Required.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Optional grouping, written to <c>service.namespace</c>.</summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Version written to <c>service.version</c>. Unset falls back to the entry assembly
    /// version, and is simply omitted when the host has no entry assembly.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>Instance identity written to <c>service.instance.id</c>. Unset is auto-generated.</summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary>Environment written to <c>deployment.environment</c>, for example <c>production</c>.</summary>
    public string? DeploymentEnvironment { get; set; }

    /// <summary>Extra resource attributes stamped onto every signal this process emits.</summary>
    public IDictionary<string, string> ResourceAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Names of the <see cref="System.Diagnostics.ActivitySource"/> instances the consumer
    /// emits spans from. A source not listed here produces no spans.
    /// </summary>
    public IList<string> ActivitySources { get; set; } = new List<string>();

    /// <summary>
    /// Names of the <see cref="System.Diagnostics.Metrics.Meter"/> instances the consumer
    /// emits measurements from. A meter not listed here produces no metrics.
    /// </summary>
    public IList<string> Meters { get; set; } = new List<string>();

    /// <summary>Head sampling settings.</summary>
    public ObservabilitySamplingOptions Sampling { get; set; } = new();

    /// <summary>Which baggage entries may be copied onto spans and log records.</summary>
    public ObservabilityBaggageOptions Baggage { get; set; } = new();

    /// <summary>Log events that must always reach stdout, whatever the log levels say.</summary>
    public ObservabilityAuditOptions Audit { get; set; } = new();

    /// <summary>Which built-in instrumentations to enable.</summary>
    public ObservabilityInstrumentationOptions Instrumentation { get; set; } = new();

    /// <summary>Where the signals are exported.</summary>
    public ObservabilityExportOptions Export { get; set; } = new();
}
