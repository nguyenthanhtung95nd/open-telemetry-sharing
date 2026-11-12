using System.Diagnostics.Metrics;

namespace Shared.Messaging;

/// <summary>
/// Instruments owned by the messaging transport. Every service that publishes or consumes
/// gets these for free, so broker health is measured the same way across the estate.
/// </summary>
public static class MessagingDiagnostics
{
    /// <summary>Meter name, which a consumer registers with the SDK.</summary>
    public const string MeterName = "Shared.Messaging";

    /// <summary>Meter carrying the transport metrics.</summary>
    public static readonly Meter Meter = new(MeterName);

    /// <summary>
    /// Publishes that threw. A non-zero rate means integration events are being lost and
    /// sagas are stalling, which is the one broker signal that always deserves an alert.
    /// </summary>
    public static readonly Counter<long> PublishFailuresCounter =
        Meter.CreateCounter<long>("messaging.publish_failures");

    /// <summary>
    /// Wall-clock time a consumer spent handling one message, tagged by queue and outcome.
    /// This is the only per-step latency available for a saga that runs over a broker.
    /// </summary>
    public static readonly Histogram<double> ConsumeDurationHistogram =
        Meter.CreateHistogram<double>("messaging.consume.duration", unit: "ms");

    /// <summary>
    /// Messages a handler could not process even after retrying, so they were routed to
    /// the dead-letter queue. Each one is a saga step that stopped and a human decision
    /// about whether to replay it.
    /// </summary>
    public static readonly Counter<long> DeadLetteredCounter =
        Meter.CreateCounter<long>("messaging.dead_lettered");
}
