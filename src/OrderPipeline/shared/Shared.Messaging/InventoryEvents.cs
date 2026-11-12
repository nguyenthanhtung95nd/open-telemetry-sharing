namespace Shared.Messaging;

public record StockReservedEvent(int OrderId, decimal Amount, IReadOnlyList<OrderLineItem> Items);

public record StockReservationFailedEvent(int OrderId, string Reason);
