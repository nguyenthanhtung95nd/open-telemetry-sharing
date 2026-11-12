using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging;

public class RabbitMqConsumer<T>
{
    /// <summary>Exchange every dead-lettered message is routed to.</summary>
    public const string DeadLetterExchange = "orderpipeline.dead-letter";

    /// <summary>Queue that collects dead-lettered messages from every service.</summary>
    public const string DeadLetterQueue = "orderpipeline.dead-letter";

    private const int ConnectAttempts = 30;
    private const ushort PrefetchCount = 10;
    private const string SucceededOutcome = "succeeded";
    private const string FailedOutcome = "failed";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly MessageDispatcher<T> _dispatcher;
    private readonly ILogger<RabbitMqConsumer<T>> _logger;
    private readonly ConnectionFactory _factory;

    public RabbitMqConsumer(string connectionString, IEventHandler<T> handler, ILogger<RabbitMqConsumer<T>> logger)
    {
        _logger = logger;
        _dispatcher = new MessageDispatcher<T>(handler, logger);
        _factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,

            // The handler is async and the delivery may only be acknowledged once it has
            // finished. EventingBasicConsumer does not await an async callback, so without
            // this the ack would race ahead of the work it claims to acknowledge.
            DispatchConsumersAsync = true
        };
    }

    public void StartConsuming(string exchange, string queueName)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Consume(exchange, queueName);
                if (attempt > 1) _logger.ConsumerRecovered(exchange, queueName, attempt);
                return;
            }
            catch (Exception ex) when (attempt < ConnectAttempts)
            {
                _logger.BrokerNotReady(exchange, queueName, attempt, ConnectAttempts, ex.Message);
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private void Consume(string exchange, string queueName)
    {
        var connection = _factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);
        DeclareDeadLetterTopology(channel);

        var queueResult = channel.QueueDeclare(queueName, true, false, false, null);
        channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);

        // Without this a consumer pulls the whole queue into memory the moment it attaches,
        // and no second instance can share the work.
        channel.BasicQos(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (_, @event) => OnReceivedAsync(channel, queueName, @event);

        channel.BasicConsume(queue: queueResult.QueueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// Declares where dead-lettered messages land. These names are new, so declaring them
    /// is safe. The queues that route to them are deliberately not touched here: a broker
    /// policy points them at this exchange, because redeclaring a live queue with different
    /// arguments raises PRECONDITION_FAILED.
    /// </summary>
    private static void DeclareDeadLetterTopology(IModel channel)
    {
        channel.ExchangeDeclare(DeadLetterExchange, ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(DeadLetterQueue, DeadLetterExchange, routingKey: string.Empty);
    }

    private async Task OnReceivedAsync(IModel channel, string queueName, BasicDeliverEventArgs @event)
    {
        using var activity = StartConsumeActivity(queueName, @event);
        var startedAt = Stopwatch.GetTimestamp();
        var outcome = SucceededOutcome;

        try
        {
            var data = Deserialise(queueName, @event);
            var dispatch = await _dispatcher.DispatchAsync(data, queueName, @event.DeliveryTag);

            if (dispatch == MessageDispatchOutcome.DeadLettered)
            {
                outcome = FailedOutcome;
                activity?.SetStatus(ActivityStatusCode.Error, "Handler failed, message dead-lettered");
                channel.BasicNack(@event.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            channel.BasicAck(@event.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // A message that cannot be deserialised can never succeed, so requeueing it
            // would loop forever. It goes straight to the dead-letter queue.
            outcome = FailedOutcome;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.MessageProcessingFailed(ex, queueName, typeof(T).Name, @event.DeliveryTag);
            _logger.MessageDeadLettered(queueName, typeof(T).Name, @event.DeliveryTag);
            channel.BasicNack(@event.DeliveryTag, multiple: false, requeue: false);
        }
        finally
        {
            RecordDuration(startedAt, queueName, outcome);
        }
    }

    private static T Deserialise(string queueName, BasicDeliverEventArgs @event)
    {
        var message = Encoding.UTF8.GetString(@event.Body.ToArray());
        return JsonSerializer.Deserialize<T>(message)
               ?? throw new InvalidOperationException($"Message on queue {queueName} deserialised to null.");
    }

    private static Activity? StartConsumeActivity(string queueName, BasicDeliverEventArgs @event)
    {
        var parentContext = RabbitMqDiagnostics.Propagator.Extract(
            default, @event.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        const string operation = "process";
        var activity = RabbitMqDiagnostics.ActivitySource.StartActivity(
            $"{queueName} {operation}", ActivityKind.Consumer, parentContext.ActivityContext);

        SetActivityContext(activity, queueName, operation);
        return activity;
    }

    private static void RecordDuration(long startedAt, string queueName, string outcome) =>
        MessagingDiagnostics.ConsumeDurationHistogram.Record(
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
            new KeyValuePair<string, object?>(MessagingTagNames.MessagingDestinationName, queueName),
            new KeyValuePair<string, object?>(MessagingTagNames.MessagingOutcome, outcome));

    private static void SetActivityContext(Activity? activity, string destinationName, string operation)
    {
        if (activity is null) return;

        activity.SetTag(MessagingTagNames.MessagingSystem, "rabbitmq");
        activity.SetTag(MessagingTagNames.MessagingDestinationKind, "queue");
        activity.SetTag(MessagingTagNames.MessagingOperation, operation);
        activity.SetTag(MessagingTagNames.MessagingDestinationName, destinationName);
    }

    private static IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        if (!props.Headers.TryGetValue(key, out var value) || value is not byte[] bytes) return [];

        return [Encoding.UTF8.GetString(bytes)];
    }
}
