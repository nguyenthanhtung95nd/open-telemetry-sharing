using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class PaymentFailedConsumerHostedService(
    IConfiguration configuration,
    IEventHandler<PaymentFailedEvent> handler,
    ILogger<RabbitMqConsumer<PaymentFailedEvent>> consumerLogger,
    ILogger<PaymentFailedConsumerHostedService> logger) : BackgroundService
{
    private const string PaymentFailedExchange = "payment.failed";
    private const string QueueName = "orderpipeline.payment-failed";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:RabbitMq must be configured.");
            var consumer = new RabbitMqConsumer<PaymentFailedEvent>(connectionString, handler, consumerLogger);
            consumer.StartConsuming(PaymentFailedExchange, QueueName);
        }
        catch (Exception ex)
        {
            logger.ConsumerFailedToStart(ex, PaymentFailedExchange, QueueName);
        }

        return Task.CompletedTask;
    }
}
