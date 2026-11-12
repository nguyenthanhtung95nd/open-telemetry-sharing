using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace Shared.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly ConnectionFactory _factory;

    public RabbitMqPublisher(string connectionString)
    {
        _factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };
    }

    /// <summary>Publishes <paramref name="event"/>, counting the failure if it throws.</summary>
    public void Publish<T>(T @event, string exchange)
    {
        try
        {
            PublishCore(@event, exchange);
        }
        catch (Exception ex)
        {
            RecordFailure(ex, @event, exchange);
            throw;
        }
    }

    // Failure is recorded here rather than in each caller's catch block, so every publisher
    // in every service reports it the same way and a single alert covers all of them.
    private static void RecordFailure<T>(Exception exception, T @event, string exchange)
    {
        var activity = Activity.Current;
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);

        MessagingDiagnostics.PublishFailuresCounter.Add(
            1,
            new KeyValuePair<string, object?>(MessagingTagNames.MessagingDestinationName, exchange),
            new KeyValuePair<string, object?>(MessagingTagNames.MessagingEventType, typeof(T).Name));
    }

    private void PublishCore<T>(T @event, string exchange)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);

        const string operation = "publish";
        var eventType = @event!.GetType().Name;
        var activityName = $"{eventType} {operation}";
        using var activity = RabbitMqDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Producer);

        ActivityContext contextToInject = default;

        if (activity != null)
        {
            contextToInject = activity.Context;
        }
        else if (Activity.Current != null)
        {
            contextToInject = Activity.Current.Context;
        }

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;

        RabbitMqDiagnostics.Propagator.Inject(
            new PropagationContext(contextToInject, Baggage.Current),
            properties,
            InjectTraceContextIntoBasicProperties);

        SetActivityContext(activity, eventType, operation);

        channel.BasicPublish(exchange: exchange, routingKey: string.Empty, basicProperties: properties, body: body);
    }

    private void SetActivityContext(Activity? activity, string eventType, string operation)
    {
        if (activity is null) return;

        activity.SetTag(MessagingTagNames.MessagingSystem, "rabbitmq");
        activity.SetTag(MessagingTagNames.MessagingDestinationKind, "exchange");
        activity.SetTag(MessagingTagNames.MessagingOperation, operation);
        activity.SetTag(MessagingTagNames.MessagingDestinationName, eventType);
    }

    private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
    {
        props.Headers ??= new Dictionary<string, object>();
        props.Headers[key] = value;
    }
}
