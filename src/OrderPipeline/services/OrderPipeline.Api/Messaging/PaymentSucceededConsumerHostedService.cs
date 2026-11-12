using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class PaymentSucceededConsumerHostedService(
    IConfiguration configuration,
    IEventHandler<PaymentSucceededEvent> handler,
    ILogger<RabbitMqConsumer<PaymentSucceededEvent>> consumerLogger,
    ILogger<PaymentSucceededConsumerHostedService> logger) : BackgroundService
{
    private const string PaymentSucceededExchange = "payment.succeeded";
    private const string QueueName = "orderpipeline.payment-succeeded";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:RabbitMq must be configured.");
            var consumer = new RabbitMqConsumer<PaymentSucceededEvent>(connectionString, handler, consumerLogger);
            consumer.StartConsuming(PaymentSucceededExchange, QueueName);
        }
        catch (Exception ex)
        {
            logger.ConsumerFailedToStart(ex, PaymentSucceededExchange, QueueName);
        }

        return Task.CompletedTask;
    }
}
