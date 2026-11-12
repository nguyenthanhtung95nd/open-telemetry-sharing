using OrderPipeline.Api.Domain;

namespace OrderPipeline.Api.Logging;

/// <summary>
/// Every log message OrderPipeline.Api emits.
/// </summary>
/// <remarks>
/// No message here may carry personally identifiable information. Log records are shipped to
/// a shared backend and retained far longer than the request that produced them.
/// </remarks>
public static partial class OrderPipelineLog
{
    /// <summary>Logs that an order was accepted and persisted.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Identifier of the new order.</param>
    /// <param name="itemCount">Number of line items on the order.</param>
    /// <param name="totalAmount">Monetary total of the order.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderCreated,
        Level = LogLevel.Information,
        Message = "Order {OrderId} created with {ItemCount} items totalling {TotalAmount}")]
    public static partial void OrderCreated(
        this ILogger logger, int orderId, int itemCount, decimal totalAmount);

    /// <summary>Logs that an order was refused before being persisted.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="reason">Why the order was refused.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderCreationFailed,
        Level = LogLevel.Warning,
        Message = "Order creation refused: {Reason}")]
    public static partial void OrderCreationFailed(this ILogger logger, string reason);

    /// <summary>Logs that a caller asked for an order they cannot see.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Identifier that was asked for.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderNotFound,
        Level = LogLevel.Warning,
        Message = "Order {OrderId} not found for the requesting user")]
    public static partial void OrderNotFound(this ILogger logger, int orderId);

    /// <summary>Logs that a saga event arrived for an unknown order.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="eventType">Name of the event contract that arrived.</param>
    /// <param name="orderId">Identifier the event referred to.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderUnknownForEvent,
        Level = LogLevel.Warning,
        Message = "Received {EventType} for unknown order {OrderId}")]
    public static partial void OrderUnknownForEvent(
        this ILogger logger, string eventType, int orderId);

    /// <summary>Logs that an order moved to a new status.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Identifier of the order.</param>
    /// <param name="status">Status the order moved to.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderStatusChanged,
        Level = LogLevel.Information,
        Message = "Order {OrderId} moved to {Status}")]
    public static partial void OrderStatusChanged(
        this ILogger logger, int orderId, OrderStatus status);

    /// <summary>Logs that a saga event was ignored because the order had already settled.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="eventType">Name of the event contract that arrived.</param>
    /// <param name="orderId">Identifier of the order.</param>
    /// <param name="status">Status the order was already in.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderStatusChangeIgnored,
        Level = LogLevel.Information,
        Message = "Ignoring {EventType} for order {OrderId}: already {Status}")]
    public static partial void OrderStatusChangeIgnored(
        this ILogger logger, string eventType, int orderId, OrderStatus status);

    /// <summary>Logs that an integration event could not be published.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The failure reported by the broker.</param>
    /// <param name="eventType">Name of the event contract being published.</param>
    /// <param name="orderId">Identifier of the order the event belonged to.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderEventPublishFailed,
        Level = LogLevel.Error,
        Message = "Could not publish {EventType} for order {OrderId}. The saga stalls until it is replayed.")]
    public static partial void OrderEventPublishFailed(
        this ILogger logger, Exception exception, string eventType, int orderId);

    /// <summary>Logs that Inventory.Api could not be reached.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The transport failure.</param>
    /// <param name="productId">Product whose availability was being checked.</param>
    [LoggerMessage(
        EventId = LogEventIds.InventoryUnreachable,
        Level = LogLevel.Error,
        Message = "Could not reach Inventory.Api while checking product {ProductId}")]
    public static partial void InventoryUnreachable(
        this ILogger logger, Exception exception, int productId);

    /// <summary>Logs that an order could not be written to the database.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The database failure.</param>
    /// <param name="orderId">Identifier of the order being saved.</param>
    [LoggerMessage(
        EventId = LogEventIds.OrderPersistenceFailed,
        Level = LogLevel.Error,
        Message = "Could not save order {OrderId} to the database")]
    public static partial void OrderPersistenceFailed(
        this ILogger logger, Exception exception, int orderId);

    /// <summary>Logs that a controller action received a request.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="action">Name of the action that was reached.</param>
    [LoggerMessage(
        EventId = LogEventIds.RequestReceived,
        Level = LogLevel.Debug,
        Message = "Handling {Action}")]
    public static partial void RequestReceived(this ILogger logger, string action);
}
