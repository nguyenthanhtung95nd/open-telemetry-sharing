using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OrderPipeline.Api.Diagnostics;

/// <summary>Instruments owned by this service, registered with the SDK in Program.cs.</summary>
public static class ApplicationDiagnostics
{
    private const string ServiceName = "OrderPipeline.Api";

    /// <summary>Meter carrying this service's business metrics.</summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Activity source for spans this service starts by hand.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>Orders accepted and persisted.</summary>
    public static readonly Counter<long> OrdersCreatedCounter =
        Meter.CreateCounter<long>("orders.created");

    /// <summary>Orders that reached a terminal status, tagged by which one.</summary>
    public static readonly Counter<long> OrdersTerminalCounter =
        Meter.CreateCounter<long>("orders.terminal");

    /// <summary>
    /// Orders accepted but not yet settled. Rises on creation and falls on every terminal
    /// transition, so a value that stays high means the saga has stalled.
    /// </summary>
    /// <remarks>
    /// This is process-local and resets on restart, which is inherent to an UpDownCounter.
    /// Alert on it staying above zero for a window rather than on its absolute value.
    /// </remarks>
    public static readonly UpDownCounter<long> OrdersInFlightCounter =
        Meter.CreateUpDownCounter<long>("orders.in_flight");

    /// <summary>
    /// Wall-clock time from an order being accepted to it reaching a terminal status.
    /// </summary>
    /// <remarks>
    /// The saga runs over a broker, so no HTTP request duration covers it. This is the only
    /// end-to-end latency signal the business has.
    /// </remarks>
    public static readonly Histogram<double> FulfilmentDurationHistogram =
        Meter.CreateHistogram<double>("orders.fulfilment.duration", unit: "ms");
}
