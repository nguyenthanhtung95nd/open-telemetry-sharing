namespace Shared.Observability.Options;

/// <summary>
/// Which built-in instrumentations the library turns on. All default to enabled so a
/// consumer gets a production-shaped baseline without naming anything.
/// </summary>
public sealed class ObservabilityInstrumentationOptions
{
    /// <summary>Incoming HTTP server spans and metrics. Harmless in a non-web host.</summary>
    public bool AspNetCore { get; set; } = true;

    /// <summary>Outgoing <see cref="System.Net.Http.HttpClient"/> spans and metrics.</summary>
    public bool HttpClient { get; set; } = true;

    /// <summary>GC, JIT, thread pool and exception counters.</summary>
    public bool Runtime { get; set; } = true;
}
