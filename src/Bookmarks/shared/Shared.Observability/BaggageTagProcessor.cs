using System.Diagnostics;
using OpenTelemetry;
using Shared.Observability.Options;

namespace Shared.Observability;

/// <summary>
/// Copies allow-listed baggage entries onto every span the process produces, so a value set
/// once at the edge of a request is queryable on spans emitted downstream.
/// </summary>
/// <remarks>
/// Baggage crosses service boundaries in a plaintext header that any caller can write, so
/// this processor copies only the key prefixes the consumer named, caps how many entries one
/// span may receive, and truncates oversized values.
/// </remarks>
public sealed class BaggageTagProcessor : BaseProcessor<Activity>
{
    private readonly ObservabilityBaggageOptions _options;

    /// <summary>Creates a processor bound to the consumer's baggage policy.</summary>
    /// <param name="options">The allow-list and the limits to enforce.</param>
    public BaggageTagProcessor(ObservabilityBaggageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Copies the allow-listed baggage onto <paramref name="data"/> before export.</summary>
    public override void OnEnd(Activity data)
    {
        if (data is not null && _options.AllowedKeyPrefixes.Count > 0)
        {
            CopyAllowedBaggage(data);
        }

        base.OnEnd(data!);
    }

    private void CopyAllowedBaggage(Activity activity)
    {
        var copied = 0;

        foreach (var entry in Baggage.Current)
        {
            if (copied >= _options.MaxEntries) return;
            if (!IsAllowed(entry.Key)) continue;

            activity.SetTag(entry.Key, Truncate(entry.Value));
            copied++;
        }
    }

    private bool IsAllowed(string key) =>
        _options.AllowedKeyPrefixes.Any(prefix =>
            key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private string Truncate(string value) =>
        value.Length <= _options.MaxValueLength ? value : value[.._options.MaxValueLength];
}
