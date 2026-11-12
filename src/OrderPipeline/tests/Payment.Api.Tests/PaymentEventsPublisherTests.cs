using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Payment.Api.Messaging;
using Shared.Messaging;

namespace Payment.Api.Tests;

/// <summary>
/// Characterization tests for <see cref="PaymentEventsPublisher"/> as it ships today.
/// </summary>
public class PaymentEventsPublisherTests
{
    private static readonly IReadOnlyList<OrderLineItem> Items = [new OrderLineItem(7, 2)];

    private readonly IRabbitMqPublisher _rabbitMqPublisher = Substitute.For<IRabbitMqPublisher>();
    private readonly PaymentEventsPublisher _sut;

    public PaymentEventsPublisherTests()
    {
        _sut = new PaymentEventsPublisher(
            _rabbitMqPublisher, NullLogger<PaymentEventsPublisher>.Instance);
    }

    [Theory]
    [InlineData("payment.succeeded")]
    public void PublishPaymentSucceeded_SendsToTheSuccessExchange(string exchange)
    {
        // Act
        _sut.PublishPaymentSucceeded(4821);

        // Assert
        _rabbitMqPublisher.Received(1).Publish(Arg.Any<PaymentSucceededEvent>(), exchange);
    }

    [Theory]
    [InlineData("payment.failed")]
    public void PublishPaymentFailed_SendsToTheFailureExchange(string exchange)
    {
        // Act
        _sut.PublishPaymentFailed(4821, Items);

        // Assert
        _rabbitMqPublisher.Received(1).Publish(Arg.Any<PaymentFailedEvent>(), exchange);
    }

    [Fact]
    public void PublishPaymentSucceeded_WhenTheBrokerThrows_SwallowsTheException()
    {
        // BUG: the payment row is already committed as Succeeded. Swallowing here means the
        // customer has been charged and the order never leaves StockReserved.

        // Arrange
        ThrowOnPublish();

        // Act
        var exception = Record.Exception(() => _sut.PublishPaymentSucceeded(4821));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void PublishPaymentFailed_WhenTheBrokerThrows_SwallowsTheException()
    {
        // BUG: compensation never runs, so Inventory keeps the stock decremented for an
        // order that will never be paid.

        // Arrange
        ThrowOnPublish();

        // Act
        var exception = Record.Exception(() => _sut.PublishPaymentFailed(4821, Items));

        // Assert
        Assert.Null(exception);
    }

    private void ThrowOnPublish() =>
        _rabbitMqPublisher
            .When(publisher => publisher.Publish(Arg.Any<object>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("broker unreachable"));
}
