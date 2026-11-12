using OpenTelemetry;
using OpenTelemetry.Logs;
using Shared.Observability.Options;

namespace Shared.Observability;

/// <summary>
/// Copies allow-listed baggage entries onto every log record, so a correlation value set
/// once at the edge of a request can be filtered on in the log backend without the call
/// site having to repeat it in a message template.
/// </summary>
/// <remarks>
/// This is the log-signal counterpart of <see cref="BaggageTagProcessor"/>. It shares the
/// same options, and therefore the same allow-list and the same limits, because baggage is
/// untrusted input wherever it is read.
/// </remarks>
public sealed class BaggageLogProcessor : BaseProcessor<LogRecord>
{
    private readonly ObservabilityBaggageOptions _options;

    /// <summary>Creates a processor bound to the consumer's baggage policy.</summary>
    /// <param name="options">The allow-list and the limits to enforce.</param>
    public BaggageLogProcessor(ObservabilityBaggageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Appends the allow-listed baggage to <paramref name="data"/> before export.</summary>
    public override void OnEnd(LogRecord data)
    {
        if (data is null || _options.AllowedKeyPrefixes.Count == 0)
        {
            base.OnEnd(data!);
            return;
        }

        var allowed = CollectAllowedBaggage();
        if (allowed.Count > 0)
        {
            data.Attributes = Merge(data.Attributes, allowed);
        }

        base.OnEnd(data);
    }

    private List<KeyValuePair<string, object?>> CollectAllowedBaggage()
    {
        var collected = new List<KeyValuePair<string, object?>>();

        foreach (var entry in Baggage.Current)
        {
            if (collected.Count >= _options.MaxEntries) break;
            if (!IsAllowed(entry.Key)) continue;

            collected.Add(new KeyValuePair<string, object?>(entry.Key, Truncate(entry.Value)));
        }

        return collected;
    }

    private bool IsAllowed(string key) =>
        _options.AllowedKeyPrefixes.Any(prefix =>
            key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private string Truncate(string value) =>
        value.Length <= _options.MaxValueLength ? value : value[.._options.MaxValueLength];

    private static List<KeyValuePair<string, object?>> Merge(
        IReadOnlyList<KeyValuePair<string, object?>>? existing,
        List<KeyValuePair<string, object?>> added)
    {
        if (existing is null || existing.Count == 0) return added;

        var merged = new List<KeyValuePair<string, object?>>(existing.Count + added.Count);
        merged.AddRange(existing);
        merged.AddRange(added);
        return merged;
    }
}
