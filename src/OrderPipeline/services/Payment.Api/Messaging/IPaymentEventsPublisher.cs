using Shared.Messaging;

namespace Payment.Api.Messaging;

public interface IPaymentEventsPublisher
{
    void PublishPaymentSucceeded(int orderId);
    void PublishPaymentFailed(int orderId, IReadOnlyList<OrderLineItem> items);
}
