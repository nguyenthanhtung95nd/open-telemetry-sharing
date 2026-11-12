namespace Shared.Messaging;

public record PaymentSucceededEvent(int OrderId);

public record PaymentFailedEvent(int OrderId, IReadOnlyList<OrderLineItem> Items);
