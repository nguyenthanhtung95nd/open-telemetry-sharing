namespace Shared.Observability.Options;

/// <summary>
/// Head sampling settings. Defaults to <see cref="SamplingMode.Default"/>, which samples
/// nothing out in-process and leaves the decision to the collector.
/// </summary>
public sealed class ObservabilitySamplingOptions
{
    /// <summary>The sampling strategy to install. Defaults to <see cref="SamplingMode.Default"/>.</summary>
    public SamplingMode Mode { get; set; } = SamplingMode.Default;

    /// <summary>
    /// Fraction of traces to record, between 0 and 1. Only read by the ratio-based modes.
    /// </summary>
    public double Ratio { get; set; } = 1.0;
}
