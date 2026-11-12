using OrderPipeline.Api.Domain;
using Shared.Messaging;

namespace OrderPipeline.Api.Diagnostics;

/// <summary>
/// Records the order lifecycle onto the instruments in <see cref="ApplicationDiagnostics"/>.
/// </summary>
/// <remarks>
/// Recording lives here rather than at each call site so that every terminal transition
/// updates the same three instruments. A handler that forgot one of them would leave
/// <c>orders.in_flight</c> drifting upwards and make the stalled-saga alert meaningless.
/// </remarks>
public static class OrderMetrics
{
    /// <summary>Records that an order was accepted and is now in flight.</summary>
    /// <param name="order">The order that was persisted.</param>
    public static void RecordAccepted(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        ApplicationDiagnostics.OrdersCreatedCounter.Add(1);
        ApplicationDiagnostics.OrdersInFlightCounter.Add(1);
    }

    /// <summary>Records that an order reached a terminal status.</summary>
    /// <param name="order">The order that settled, carrying the time it was created.</param>
    public static void RecordSettled(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var statusTag = new KeyValuePair<string, object?>(
            OrderPipelineTags.OrderStatus, order.Status.ToString());

        ApplicationDiagnostics.OrdersTerminalCounter.Add(1, statusTag);
        ApplicationDiagnostics.OrdersInFlightCounter.Add(-1);
        ApplicationDiagnostics.FulfilmentDurationHistogram.Record(
            (DateTime.UtcNow - order.CreatedAtUtc).TotalMilliseconds, statusTag);
    }
}
