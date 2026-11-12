using Shared.Messaging;

namespace Inventory.Api.Messaging;

public interface IInventoryEventsPublisher
{
    void PublishStockReserved(int orderId, decimal amount, IReadOnlyList<OrderLineItem> items);
    void PublishStockReservationFailed(int orderId, string reason);
}
