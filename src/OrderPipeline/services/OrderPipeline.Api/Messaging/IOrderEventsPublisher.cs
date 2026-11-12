using OrderPipeline.Api.Domain;

namespace OrderPipeline.Api.Messaging;

public interface IOrderEventsPublisher
{
    void PublishOrderCreated(Order order);
}
