using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Messaging;

namespace Inventory.Api.Messaging;

public sealed class OrderCreatedConsumerHostedService(
    IConfiguration configuration,
    IEventHandler<OrderCreatedEvent> handler,
    ILogger<RabbitMqConsumer<OrderCreatedEvent>> consumerLogger,
    ILogger<OrderCreatedConsumerHostedService> logger) : BackgroundService
{
    private const string OrderEventsExchange = "order.events";
    private const string QueueName = "inventory.order-created";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:RabbitMq must be configured.");
            var consumer = new RabbitMqConsumer<OrderCreatedEvent>(connectionString, handler, consumerLogger);
            consumer.StartConsuming(OrderEventsExchange, QueueName);
        }
        catch (Exception ex)
        {
            logger.ConsumerFailedToStart(ex, OrderEventsExchange, QueueName);
        }

        return Task.CompletedTask;
    }
}
