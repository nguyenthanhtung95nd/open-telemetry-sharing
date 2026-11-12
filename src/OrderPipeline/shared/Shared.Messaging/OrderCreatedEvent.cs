namespace Shared.Messaging;

public record OrderCreatedEvent(int OrderId, string CustomerName, decimal TotalAmount, IReadOnlyList<OrderLineItem> Items);

public record OrderLineItem(int ProductId, int Quantity);
