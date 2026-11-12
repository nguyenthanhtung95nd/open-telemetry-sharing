namespace Shared.Observability.Options;

/// <summary>
/// How the SDK decides, at span creation time, whether a trace is recorded.
/// </summary>
public enum SamplingMode
{
    /// <summary>
    /// Leave the SDK default in place. Nothing is dropped in-process, and the standard
    /// <c>OTEL_TRACES_SAMPLER</c> environment variable still applies. Use this whenever a
    /// collector performs tail sampling, because a span dropped here never reaches it.
    /// </summary>
    Default = 0,

    /// <summary>Record every span.</summary>
    AlwaysOn,

    /// <summary>Record no spans.</summary>
    AlwaysOff,

    /// <summary>
    /// Record a deterministic fraction of traces, chosen from the trace id so every span of
    /// a trace agrees. Ignores the parent decision.
    /// </summary>
    TraceIdRatio,

    /// <summary>
    /// Honour an upstream parent's decision, and fall back to <see cref="TraceIdRatio"/> for
    /// traces that start here. This is the safe choice for head sampling.
    /// </summary>
    ParentBasedTraceIdRatio
}
