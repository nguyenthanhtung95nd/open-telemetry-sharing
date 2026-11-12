using Payment.Api.Logging;
using Shared.Messaging;

namespace Payment.Api.Messaging;

public sealed class PaymentEventsPublisher(IRabbitMqPublisher rabbitMqPublisher, ILogger<PaymentEventsPublisher> logger)
    : IPaymentEventsPublisher
{
    private const string PaymentSucceededExchange = "payment.succeeded";
    private const string PaymentFailedExchange = "payment.failed";

    public void PublishPaymentSucceeded(int orderId)
    {
        try
        {
            rabbitMqPublisher.Publish(new PaymentSucceededEvent(orderId), PaymentSucceededExchange);
        }
        catch (Exception ex)
        {
            logger.PaymentEventPublishFailed(ex, nameof(PaymentSucceededEvent), orderId);
        }
    }

    public void PublishPaymentFailed(int orderId, IReadOnlyList<OrderLineItem> items)
    {
        try
        {
            rabbitMqPublisher.Publish(new PaymentFailedEvent(orderId, items), PaymentFailedExchange);
        }
        catch (Exception ex)
        {
            logger.PaymentEventPublishFailed(ex, nameof(PaymentFailedEvent), orderId);
        }
    }
}
