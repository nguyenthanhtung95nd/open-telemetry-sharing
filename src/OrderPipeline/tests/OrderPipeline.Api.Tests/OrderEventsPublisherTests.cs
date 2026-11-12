using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Messaging;
using Shared.Messaging;

namespace OrderPipeline.Api.Tests;

/// <summary>
/// Characterization tests for <see cref="OrderEventsPublisher"/> as it ships today.
/// </summary>
public class OrderEventsPublisherTests
{
    private readonly IRabbitMqPublisher _rabbitMqPublisher = Substitute.For<IRabbitMqPublisher>();
    private readonly OrderEventsPublisher _sut;

    public OrderEventsPublisherTests()
    {
        _sut = new OrderEventsPublisher(_rabbitMqPublisher, NullLogger<OrderEventsPublisher>.Instance);
    }

    [Fact]
    public void PublishOrderCreated_SendsToTheOrderEventsExchange()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        _sut.PublishOrderCreated(order);

        // Assert
        _rabbitMqPublisher.Received(1).Publish(Arg.Any<OrderCreatedEvent>(), "order.events");
    }

    [Fact]
    public void PublishOrderCreated_MapsTheOrderOntoTheEventContract()
    {
        // Arrange
        var order = CreateOrder();
        OrderCreatedEvent? published = null;
        _rabbitMqPublisher
            .When(publisher => publisher.Publish(Arg.Any<OrderCreatedEvent>(), Arg.Any<string>()))
            .Do(call => published = call.Arg<OrderCreatedEvent>());

        // Act
        _sut.PublishOrderCreated(order);

        // Assert
        Assert.Equal(order.Id, published!.OrderId);
        Assert.Equal(order.TotalAmount, published.TotalAmount);
        Assert.Equal(order.Items.Count, published.Items.Count);
    }

    [Fact]
    public void PublishOrderCreated_WhenTheBrokerThrows_SwallowsTheExceptionAndReturnsNormally()
    {
        // BUG: the order row is already committed at this point. Swallowing here leaves the
        // order stuck in Pending forever with no event ever reaching Inventory, and nothing
        // counts the failure, so no alert can ever fire on it.

        // Arrange
        _rabbitMqPublisher
            .When(publisher => publisher.Publish(Arg.Any<OrderCreatedEvent>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("broker unreachable"));

        // Act
        var exception = Record.Exception(() => _sut.PublishOrderCreated(CreateOrder()));

        // Assert
        Assert.Null(exception);
    }

    private static Order CreateOrder() => new()
    {
        Id = 4821,
        CustomerName = "Test Customer",
        CustomerEmail = "test@example.com",
        Status = OrderStatus.Pending,
        CreatedAtUtc = DateTime.UtcNow,
        TotalAmount = 250m,
        Items = [new OrderItem { ProductId = 7, Quantity = 2, UnitPrice = 125m }]
    };
}
