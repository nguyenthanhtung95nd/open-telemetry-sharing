namespace Inventory.Api.Domain;

/// <summary>
/// Records that one order has reserved stock.
/// </summary>
/// <remarks>
/// Keyed by order id, so the database itself refuses a second reservation for the same
/// order however many times <c>OrderCreatedEvent</c> is delivered. It also answers which
/// orders are currently holding stock, which nothing else in the estate can answer.
/// </remarks>
public class StockReservation
{
    /// <summary>Order the reservation belongs to. This is the primary key.</summary>
    public int OrderId { get; set; }

    /// <summary>When the stock was taken, used to find reservations that never settled.</summary>
    public DateTime ReservedAtUtc { get; set; }
}
