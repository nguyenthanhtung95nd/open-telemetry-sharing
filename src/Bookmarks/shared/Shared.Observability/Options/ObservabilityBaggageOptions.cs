namespace Shared.Observability.Options;

/// <summary>
/// Controls which baggage entries are copied onto spans. Baggage arrives from callers over
/// the W3C <c>baggage</c> header, so it is untrusted input and is opt-in by key prefix.
/// </summary>
public sealed class ObservabilityBaggageOptions
{
    /// <summary>Default cap on how many baggage entries reach a single span.</summary>
    public const int DefaultMaxEntries = 16;

    /// <summary>Default cap on the length of a single copied baggage value.</summary>
    public const int DefaultMaxValueLength = 256;

    /// <summary>
    /// Key prefixes a consumer allows onto spans, compared case-insensitively. Empty means
    /// no baggage is copied at all, which is the safe default.
    /// </summary>
    public IList<string> AllowedKeyPrefixes { get; set; } = new List<string>();

    /// <summary>Maximum number of allowed entries copied onto one span.</summary>
    public int MaxEntries { get; set; } = DefaultMaxEntries;

    /// <summary>Values longer than this are truncated before being written to a span.</summary>
    public int MaxValueLength { get; set; } = DefaultMaxValueLength;
}
