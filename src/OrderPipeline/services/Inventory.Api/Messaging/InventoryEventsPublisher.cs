using Inventory.Api.Logging;
using Shared.Messaging;

namespace Inventory.Api.Messaging;

public sealed class InventoryEventsPublisher(IRabbitMqPublisher rabbitMqPublisher, ILogger<InventoryEventsPublisher> logger)
    : IInventoryEventsPublisher
{
    private const string StockReservedExchange = "inventory.stock-reserved";
    private const string StockReservationFailedExchange = "inventory.stock-reservation-failed";

    public void PublishStockReserved(int orderId, decimal amount, IReadOnlyList<OrderLineItem> items)
    {
        try
        {
            rabbitMqPublisher.Publish(new StockReservedEvent(orderId, amount, items), StockReservedExchange);
        }
        catch (Exception ex)
        {
            logger.InventoryEventPublishFailed(ex, nameof(StockReservedEvent), orderId);
        }
    }

    public void PublishStockReservationFailed(int orderId, string reason)
    {
        try
        {
            rabbitMqPublisher.Publish(new StockReservationFailedEvent(orderId, reason), StockReservationFailedExchange);
        }
        catch (Exception ex)
        {
            logger.InventoryEventPublishFailed(ex, nameof(StockReservationFailedEvent), orderId);
        }
    }
}
