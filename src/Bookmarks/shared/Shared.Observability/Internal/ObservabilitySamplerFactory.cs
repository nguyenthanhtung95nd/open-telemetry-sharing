using OpenTelemetry.Trace;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>Maps a <see cref="SamplingMode"/> onto an SDK sampler.</summary>
internal static class ObservabilitySamplerFactory
{
    /// <summary>
    /// Returns the sampler to install, or <see langword="null"/> for
    /// <see cref="SamplingMode.Default"/>, which leaves the SDK default untouched so that
    /// <c>OTEL_TRACES_SAMPLER</c> keeps working.
    /// </summary>
    public static Sampler? Create(ObservabilitySamplingOptions sampling) => sampling.Mode switch
    {
        SamplingMode.AlwaysOn => new AlwaysOnSampler(),
        SamplingMode.AlwaysOff => new AlwaysOffSampler(),
        SamplingMode.TraceIdRatio => new TraceIdRatioBasedSampler(sampling.Ratio),
        SamplingMode.ParentBasedTraceIdRatio =>
            new ParentBasedSampler(new TraceIdRatioBasedSampler(sampling.Ratio)),
        _ => null
    };
}
