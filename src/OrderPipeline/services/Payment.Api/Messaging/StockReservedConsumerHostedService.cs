using Shared.Messaging;

namespace Payment.Api.Messaging;

public sealed class StockReservedConsumerHostedService(
    IConfiguration configuration,
    IEventHandler<StockReservedEvent> handler,
    ILogger<RabbitMqConsumer<StockReservedEvent>> consumerLogger,
    ILogger<StockReservedConsumerHostedService> logger) : BackgroundService
{
    private const string StockReservedExchange = "inventory.stock-reserved";
    private const string QueueName = "payment.stock-reserved";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:RabbitMq must be configured.");
            var consumer = new RabbitMqConsumer<StockReservedEvent>(connectionString, handler, consumerLogger);
            consumer.StartConsuming(StockReservedExchange, QueueName);
        }
        catch (Exception ex)
        {
            logger.ConsumerFailedToStart(ex, StockReservedExchange, QueueName);
        }

        return Task.CompletedTask;
    }
}
