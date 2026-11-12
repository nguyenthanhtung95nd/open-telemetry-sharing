using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Logging;
using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class OrderEventsPublisher(IRabbitMqPublisher rabbitMqPublisher, ILogger<OrderEventsPublisher> logger)
    : IOrderEventsPublisher
{
    private const string OrderEventsExchange = "order.events";

    public void PublishOrderCreated(Order order)
    {
        try
        {
            var @event = new OrderCreatedEvent(
                order.Id,
                order.CustomerName,
                order.TotalAmount,
                order.Items.Select(i => new OrderLineItem(i.ProductId, i.Quantity)).ToList());

            rabbitMqPublisher.Publish(@event, OrderEventsExchange);
        }
        catch (Exception ex)
        {
            logger.OrderEventPublishFailed(ex, nameof(OrderCreatedEvent), order.Id);
        }
    }
}
