using Microsoft.Extensions.Logging;

namespace Shared.Messaging;

/// <summary>
/// Every log message the messaging transport emits.
/// </summary>
/// <remarks>
/// Event ids 900 to 999 are reserved for shared infrastructure, leaving each service its
/// own range above 1000.
/// </remarks>
public static partial class MessagingLog
{
    /// <summary>Logs that a handler threw while processing a delivered message.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The exception the handler threw.</param>
    /// <param name="queueName">Queue the message was delivered on.</param>
    /// <param name="eventType">CLR type name of the event contract.</param>
    /// <param name="deliveryTag">Broker delivery tag, for matching against the broker log.</param>
    [LoggerMessage(
        EventId = 901,
        Level = LogLevel.Error,
        Message = "Handler threw while processing {EventType} on queue {QueueName} (delivery {DeliveryTag})")]
    public static partial void MessageProcessingFailed(
        this ILogger logger, Exception exception, string queueName, string eventType, ulong deliveryTag);

    /// <summary>Logs that a handler threw on an attempt that will be retried.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The exception the handler threw.</param>
    /// <param name="queueName">Queue the message was delivered on.</param>
    /// <param name="eventType">CLR type name of the event contract.</param>
    /// <param name="attempt">Attempt number that just failed, counting from one.</param>
    [LoggerMessage(
        EventId = 905,
        Level = LogLevel.Warning,
        Message = "Attempt {Attempt} at {EventType} on queue {QueueName} failed, retrying")]
    public static partial void MessageProcessingRetrying(
        this ILogger logger, Exception exception, string queueName, string eventType, int attempt);

    /// <summary>Logs that a message was given up on and routed to the dead-letter queue.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="queueName">Queue the message was delivered on.</param>
    /// <param name="eventType">CLR type name of the event contract.</param>
    /// <param name="deliveryTag">Broker delivery tag, for matching against the broker log.</param>
    [LoggerMessage(
        EventId = 906,
        Level = LogLevel.Error,
        Message = "Dead-lettering {EventType} from queue {QueueName} (delivery {DeliveryTag}). The saga step did not run and will not run until this is replayed.")]
    public static partial void MessageDeadLettered(
        this ILogger logger, string queueName, string eventType, ulong deliveryTag);

    /// <summary>Logs that the broker was not accepting connections yet.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exchange">Exchange being bound.</param>
    /// <param name="queueName">Queue being declared.</param>
    /// <param name="attempt">Attempt number just made.</param>
    /// <param name="maxAttempts">Total attempts that will be made.</param>
    /// <param name="reason">Message from the underlying failure.</param>
    [LoggerMessage(
        EventId = 902,
        Level = LogLevel.Warning,
        Message = "Broker not ready for {Exchange}/{QueueName} (attempt {Attempt}/{MaxAttempts}): {Reason}")]
    public static partial void BrokerNotReady(
        this ILogger logger, string exchange, string queueName, int attempt, int maxAttempts, string reason);

    /// <summary>Logs that a consumer attached after one or more failed attempts.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exchange">Exchange that was bound.</param>
    /// <param name="queueName">Queue that was declared.</param>
    /// <param name="attempts">Number of attempts it took.</param>
    [LoggerMessage(
        EventId = 903,
        Level = LogLevel.Information,
        Message = "Consuming {Exchange}/{QueueName} after {Attempts} attempts")]
    public static partial void ConsumerRecovered(
        this ILogger logger, string exchange, string queueName, int attempts);

    /// <summary>Logs that a consumer could not be started at all.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The exception that stopped the consumer starting.</param>
    /// <param name="exchange">Exchange that was being bound.</param>
    /// <param name="queueName">Queue that was being declared.</param>
    [LoggerMessage(
        EventId = 904,
        Level = LogLevel.Error,
        Message = "Consumer for {Exchange}/{QueueName} failed to start. This service keeps running without consuming that queue.")]
    public static partial void ConsumerFailedToStart(
        this ILogger logger, Exception exception, string exchange, string queueName);
}
