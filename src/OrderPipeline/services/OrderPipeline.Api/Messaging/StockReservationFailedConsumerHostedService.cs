using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class StockReservationFailedConsumerHostedService(
    IConfiguration configuration,
    IEventHandler<StockReservationFailedEvent> handler,
    ILogger<RabbitMqConsumer<StockReservationFailedEvent>> consumerLogger,
    ILogger<StockReservationFailedConsumerHostedService> logger) : BackgroundService
{
    private const string StockReservationFailedExchange = "inventory.stock-reservation-failed";
    private const string QueueName = "orderpipeline.stock-reservation-failed";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:RabbitMq must be configured.");
            var consumer = new RabbitMqConsumer<StockReservationFailedEvent>(connectionString, handler, consumerLogger);
            consumer.StartConsuming(StockReservationFailedExchange, QueueName);
        }
        catch (Exception ex)
        {
            logger.ConsumerFailedToStart(ex, StockReservationFailedExchange, QueueName);
        }

        return Task.CompletedTask;
    }
}
