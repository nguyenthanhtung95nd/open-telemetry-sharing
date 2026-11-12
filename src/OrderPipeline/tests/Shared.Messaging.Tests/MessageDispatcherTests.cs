using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Polly;
using Polly.Retry;
using Polly.Testing;
using Shared.Messaging;

namespace Shared.Messaging.Tests;

/// <summary>
/// The retry and dead-letter decision, exercised without a broker. This is the code that
/// decides whether a failing message is kept or lost, so it has to be provable in a unit
/// test rather than only by watching a live queue.
/// </summary>
public class MessageDispatcherTests
{
    private const string QueueName = "inventory.order-created";
    private const ulong DeliveryTag = 7;

    private readonly IEventHandler<TestEvent> _handler = Substitute.For<IEventHandler<TestEvent>>();
    private readonly TestEvent _message = new(42);

    [Fact]
    public async Task DispatchAsync_WhenTheHandlerSucceeds_Acknowledges()
    {
        // Act
        var outcome = await BuildDispatcher().DispatchAsync(_message, QueueName, DeliveryTag);

        // Assert
        Assert.Equal(MessageDispatchOutcome.Acknowledged, outcome);
    }

    [Fact]
    public async Task DispatchAsync_WhenTheHandlerSucceeds_RunsItOnce()
    {
        // Act
        await BuildDispatcher().DispatchAsync(_message, QueueName, DeliveryTag);

        // Assert
        await _handler.Received(1).HandleAsync(_message);
    }

    [Fact]
    public async Task DispatchAsync_WhenTheHandlerAlwaysThrows_DeadLetters()
    {
        // Arrange
        ThrowOnEveryAttempt();

        // Act
        var outcome = await BuildDispatcher().DispatchAsync(_message, QueueName, DeliveryTag);

        // Assert
        Assert.Equal(MessageDispatchOutcome.DeadLettered, outcome);
    }

    [Fact]
    public async Task DispatchAsync_WhenTheHandlerAlwaysThrows_TriesThreeTimesInTotal()
    {
        // Three attempts in total, not three retries on top of the first. A handler that
        // calls a payment gateway must not be run four times for one message.

        // Arrange
        ThrowOnEveryAttempt();

        // Act
        await BuildDispatcher().DispatchAsync(_message, QueueName, DeliveryTag);

        // Assert
        await _handler.Received(MessageDispatcher<TestEvent>.DefaultMaxAttempts).HandleAsync(_message);
    }

    [Fact]
    public async Task DispatchAsync_WhenTheHandlerThrowsTwiceThenSucceeds_Acknowledges()
    {
        // The whole point of retrying. A database failover lasting a few seconds must not
        // send an order to the dead-letter queue.

        // Arrange
        var attempts = 0;
        _handler.HandleAsync(Arg.Any<TestEvent>()).Returns(_ =>
        {
            attempts++;
            return attempts < 3
                ? Task.FromException(new InvalidOperationException("transient"))
                : Task.CompletedTask;
        });

        // Act
        var outcome = await BuildDispatcher().DispatchAsync(_message, QueueName, DeliveryTag);

        // Assert
        Assert.Equal(MessageDispatchOutcome.Acknowledged, outcome);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task DispatchAsync_DoesNotLetTheHandlerExceptionEscape()
    {
        // An escaping exception would reach the AMQP callback, where it cannot be turned
        // into a nack, and the message would be lost exactly as it was before this change.

        // Arrange
        ThrowOnEveryAttempt();

        // Act
        var exception = await Record.ExceptionAsync(
            () => BuildDispatcher().DispatchAsync(_message, QueueName, DeliveryTag));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void BuildDefaultPipeline_RetriesTwiceOnTopOfTheFirstAttempt()
    {
        // The behavioural tests above run on a pipeline with the delays removed, so this
        // is the only thing checking what production is actually configured with.

        // Act
        var retry = DescribeDefaultRetryStrategy();

        // Assert
        Assert.Equal(MessageDispatcher<TestEvent>.DefaultMaxAttempts - 1, retry.MaxRetryAttempts);
    }

    [Fact]
    public void BuildDefaultPipeline_BacksOffExponentiallyWithJitter()
    {
        // A fixed delay shared by every consumer turns a broker-wide outage into a
        // synchronised retry storm the moment it recovers.

        // Act
        var retry = DescribeDefaultRetryStrategy();

        // Assert
        Assert.Equal(DelayBackoffType.Exponential, retry.BackoffType);
        Assert.True(retry.UseJitter);
        Assert.Equal(TimeSpan.FromSeconds(1), retry.Delay);
    }

    [Fact]
    public async Task DispatchAsync_NamesTheQueueInTheRetryLine()
    {
        // The pipeline is built once per consumer, before the queue it serves is known, so
        // the name has to travel with the call. Getting this wrong logs "queue unknown" on
        // every retry, which is exactly the line an operator reaches for first.

        // Arrange
        var logger = new RecordingLogger();
        var attempts = 0;
        _handler.HandleAsync(Arg.Any<TestEvent>()).Returns(_ =>
        {
            attempts++;
            return attempts < 2 ? Task.FromException(new InvalidOperationException("transient")) : Task.CompletedTask;
        });
        var dispatcher = new MessageDispatcher<TestEvent>(
            _handler, logger, MessageDispatcher<TestEvent>.BuildDefaultPipelineWithoutDelay(logger));

        // Act
        await dispatcher.DispatchAsync(_message, QueueName, DeliveryTag);

        // Assert
        Assert.Contains(logger.Messages, message => message.Contains(QueueName));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("unknown"));
    }

    private sealed class RecordingLogger : Microsoft.Extensions.Logging.ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private static RetryStrategyOptions DescribeDefaultRetryStrategy()
    {
        var descriptor = MessageDispatcher<TestEvent>
            .BuildDefaultPipeline(NullLogger.Instance)
            .GetPipelineDescriptor();

        return Assert.IsType<RetryStrategyOptions>(Assert.Single(descriptor.Strategies).Options);
    }

    private void ThrowOnEveryAttempt() =>
        _handler.HandleAsync(Arg.Any<TestEvent>())
            .Returns(_ => Task.FromException(new InvalidOperationException("handler is broken")));

    /// <summary>The production policy with its delays removed, so a test runs instantly.</summary>
    private MessageDispatcher<TestEvent> BuildDispatcher() =>
        new(_handler, NullLogger.Instance, new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MessageDispatcher<TestEvent>.DefaultMaxAttempts - 1,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant
            })
            .Build());

    public sealed record TestEvent(int OrderId);
}
