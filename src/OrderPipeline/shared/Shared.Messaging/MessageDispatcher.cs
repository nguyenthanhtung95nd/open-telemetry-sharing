using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Shared.Messaging;

/// <summary>What the broker should be told about a delivery once the handler has run.</summary>
public enum MessageDispatchOutcome
{
    /// <summary>The handler completed. Acknowledge the delivery.</summary>
    Acknowledged,

    /// <summary>The handler kept failing. Reject the delivery so the broker dead-letters it.</summary>
    DeadLettered
}

/// <summary>
/// Runs a handler against one message and decides whether the delivery can be acknowledged.
/// </summary>
/// <remarks>
/// This is separate from <see cref="RabbitMqConsumer{T}"/> on purpose. The retry and
/// dead-letter decision is the part that must be proven by tests, and it can be exercised
/// without a broker only if it does not touch one.
/// </remarks>
/// <typeparam name="T">Event contract the handler consumes.</typeparam>
public sealed class MessageDispatcher<T>
{
    /// <summary>Attempts a handler gets in total, counting the first one.</summary>
    public const int DefaultMaxAttempts = 3;

    /// <summary>
    /// Carries the queue name into the retry callback. The pipeline is built once per
    /// consumer, before the queue it will serve is known, so the name has to travel with
    /// the call rather than be captured when the policy is built.
    /// </summary>
    private static readonly ResiliencePropertyKey<string> QueueNameKey = new("queueName");

    private readonly IEventHandler<T> _handler;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>Creates a dispatcher around a handler.</summary>
    /// <param name="handler">The handler to run.</param>
    /// <param name="logger">Logger for retry and dead-letter lines.</param>
    /// <param name="retryPipeline">
    /// Retry policy to run the handler under. Pass <see langword="null"/> for the default
    /// of three attempts with exponential backoff.
    /// </param>
    public MessageDispatcher(IEventHandler<T> handler, ILogger logger, ResiliencePipeline? retryPipeline = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(logger);

        _handler = handler;
        _logger = logger;
        _retryPipeline = retryPipeline ?? BuildDefaultPipeline(logger);
    }

    /// <summary>
    /// Builds the retry policy used in production: three attempts, one second of backoff
    /// doubling each time, with jitter so a broker-wide outage does not produce a
    /// synchronised retry storm from every consumer at once.
    /// </summary>
    /// <param name="logger">Logger that receives a line per failed attempt.</param>
    /// <returns>The pipeline to run a handler under.</returns>
    public static ResiliencePipeline BuildDefaultPipeline(ILogger logger) =>
        BuildPipeline(logger, TimeSpan.FromSeconds(1));

    /// <summary>
    /// The production policy with its backoff removed, so a test can exercise the real
    /// retry callback without waiting three seconds for it.
    /// </summary>
    /// <param name="logger">Logger that receives a line per failed attempt.</param>
    /// <returns>The pipeline to run a handler under.</returns>
    public static ResiliencePipeline BuildDefaultPipelineWithoutDelay(ILogger logger) =>
        BuildPipeline(logger, TimeSpan.Zero);

    private static ResiliencePipeline BuildPipeline(ILogger logger, TimeSpan delay) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = DefaultMaxAttempts - 1,
                Delay = delay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = arguments =>
                {
                    arguments.Context.Properties.TryGetValue(QueueNameKey, out var queueName);
                    logger.MessageProcessingRetrying(
                        arguments.Outcome.Exception!,
                        queueName ?? "unknown",
                        typeof(T).Name,
                        arguments.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

    /// <summary>Runs the handler, retrying on failure, and reports what to tell the broker.</summary>
    /// <param name="message">The deserialised event.</param>
    /// <param name="queueName">Queue the delivery arrived on, for the log line.</param>
    /// <param name="deliveryTag">Broker delivery tag, for matching against the broker log.</param>
    /// <returns>Whether the delivery should be acknowledged or dead-lettered.</returns>
    public async Task<MessageDispatchOutcome> DispatchAsync(T message, string queueName, ulong deliveryTag)
    {
        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(QueueNameKey, queueName);

        try
        {
            await _retryPipeline.ExecuteAsync(
                async _ => await _handler.HandleAsync(message), context);

            return MessageDispatchOutcome.Acknowledged;
        }
        catch (Exception exception)
        {
            _logger.MessageProcessingFailed(exception, queueName, typeof(T).Name, deliveryTag);
            _logger.MessageDeadLettered(queueName, typeof(T).Name, deliveryTag);
            RecordDeadLetter(queueName);

            return MessageDispatchOutcome.DeadLettered;
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private static void RecordDeadLetter(string queueName) =>
        MessagingDiagnostics.DeadLetteredCounter.Add(
            1,
            new KeyValuePair<string, object?>(MessagingTagNames.MessagingDestinationName, queueName),
            new KeyValuePair<string, object?>(MessagingTagNames.MessagingEventType, typeof(T).Name));
}
