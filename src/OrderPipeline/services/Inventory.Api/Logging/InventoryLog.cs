namespace Inventory.Api.Logging;

/// <summary>
/// Event ids for this service. The 3000 range belongs to Inventory.Api.
/// </summary>
public static class LogEventIds
{
    /// <summary>Stock was reserved for an order.</summary>
    public const int StockReserved = 3001;

    /// <summary>Stock could not be reserved for an order.</summary>
    public const int StockReservationFailed = 3002;

    /// <summary>Stock was returned after a payment failure.</summary>
    public const int StockCompensated = 3003;

    /// <summary>Compensation referred to a product with no stock record.</summary>
    public const int CompensationProductUnknown = 3004;

    /// <summary>An integration event could not be published.</summary>
    public const int InventoryEventPublishFailed = 3005;

    /// <summary>An availability check reached the controller.</summary>
    public const int AvailabilityChecked = 3006;

    /// <summary>An administrator changed the product catalogue.</summary>
    public const int CatalogueChanged = 3007;

    /// <summary>A delivery arrived for an order whose stock was already reserved.</summary>
    public const int StockAlreadyReserved = 3008;

    /// <summary>Events that must always reach stdout, whatever the configured log levels.</summary>
    public static readonly int[] Audited =
    [
        StockReserved, StockReservationFailed, StockCompensated, CatalogueChanged,
        StockAlreadyReserved
    ];
}

/// <summary>
/// Every log message Inventory.Api emits.
/// </summary>
/// <remarks>
/// No message here may carry personally identifiable information.
/// </remarks>
public static partial class InventoryLog
{
    /// <summary>Logs that stock was reserved for an order.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order the stock was reserved for.</param>
    /// <param name="itemCount">Number of line items reserved.</param>
    [LoggerMessage(
        EventId = LogEventIds.StockReserved,
        Level = LogLevel.Information,
        Message = "Reserved stock for order {OrderId} across {ItemCount} items")]
    public static partial void StockReserved(this ILogger logger, int orderId, int itemCount);

    /// <summary>Logs that stock could not be reserved.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order the reservation was for.</param>
    /// <param name="reason">Which product fell short and by how much.</param>
    [LoggerMessage(
        EventId = LogEventIds.StockReservationFailed,
        Level = LogLevel.Warning,
        Message = "Stock reservation refused for order {OrderId}: {Reason}")]
    public static partial void StockReservationFailed(
        this ILogger logger, int orderId, string reason);

    /// <summary>Logs that a redelivery arrived for an order already holding stock.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order that was already reserved.</param>
    [LoggerMessage(
        EventId = LogEventIds.StockAlreadyReserved,
        Level = LogLevel.Information,
        Message = "Order {OrderId} already holds stock, skipping the reservation and republishing")]
    public static partial void StockAlreadyReserved(this ILogger logger, int orderId);

    /// <summary>Logs that stock was returned after a payment failure.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order whose stock was returned.</param>
    [LoggerMessage(
        EventId = LogEventIds.StockCompensated,
        Level = LogLevel.Information,
        Message = "Returned stock for order {OrderId} after payment failure")]
    public static partial void StockCompensated(this ILogger logger, int orderId);

    /// <summary>Logs that compensation referred to an unknown product.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="productId">Product that has no stock record.</param>
    /// <param name="orderId">Order being compensated.</param>
    [LoggerMessage(
        EventId = LogEventIds.CompensationProductUnknown,
        Level = LogLevel.Warning,
        Message = "Cannot return stock for unknown product {ProductId} on order {OrderId}")]
    public static partial void CompensationProductUnknown(
        this ILogger logger, int productId, int orderId);

    /// <summary>Logs that an integration event could not be published.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The failure reported by the broker.</param>
    /// <param name="eventType">Name of the event contract being published.</param>
    /// <param name="orderId">Order the event belonged to.</param>
    [LoggerMessage(
        EventId = LogEventIds.InventoryEventPublishFailed,
        Level = LogLevel.Error,
        Message = "Could not publish {EventType} for order {OrderId}. The saga stalls until it is replayed.")]
    public static partial void InventoryEventPublishFailed(
        this ILogger logger, Exception exception, string eventType, int orderId);

    /// <summary>Logs an availability check.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="productId">Product being checked.</param>
    /// <param name="quantity">Quantity asked for.</param>
    [LoggerMessage(
        EventId = LogEventIds.AvailabilityChecked,
        Level = LogLevel.Debug,
        Message = "Checking availability of product {ProductId} for quantity {Quantity}")]
    public static partial void AvailabilityChecked(
        this ILogger logger, int productId, int quantity);

    /// <summary>Logs that an administrator changed the product catalogue.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="action">What was done, for example created or deleted.</param>
    /// <param name="productId">Product that changed.</param>
    /// <param name="actor">Subject claim of the administrator who made the change.</param>
    [LoggerMessage(
        EventId = LogEventIds.CatalogueChanged,
        Level = LogLevel.Information,
        Message = "Catalogue {Action} for product {ProductId} by {Actor}")]
    public static partial void CatalogueChanged(
        this ILogger logger, string action, int productId, string actor);
}
