using Xunit;
using Inventory.Api.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Messaging;

namespace Inventory.Api.Tests;

/// <summary>
/// Characterization tests for <see cref="InventoryEventsPublisher"/> as it ships today.
/// </summary>
public class InventoryEventsPublisherTests
{
    private static readonly IReadOnlyList<OrderLineItem> Items = [new OrderLineItem(7, 2)];

    private readonly IRabbitMqPublisher _rabbitMqPublisher = Substitute.For<IRabbitMqPublisher>();
    private readonly InventoryEventsPublisher _sut;

    public InventoryEventsPublisherTests()
    {
        _sut = new InventoryEventsPublisher(
            _rabbitMqPublisher, NullLogger<InventoryEventsPublisher>.Instance);
    }

    [Fact]
    public void PublishStockReserved_SendsToTheStockReservedExchange()
    {
        // Act
        _sut.PublishStockReserved(4821, 250m, Items);

        // Assert
        _rabbitMqPublisher.Received(1)
            .Publish(Arg.Any<StockReservedEvent>(), "inventory.stock-reserved");
    }

    [Fact]
    public void PublishStockReservationFailed_SendsToTheFailureExchange()
    {
        // Act
        _sut.PublishStockReservationFailed(4821, "out of stock");

        // Assert
        _rabbitMqPublisher.Received(1)
            .Publish(Arg.Any<StockReservationFailedEvent>(), "inventory.stock-reservation-failed");
    }

    [Fact]
    public void PublishStockReserved_WhenTheBrokerThrows_SwallowsTheException()
    {
        // BUG: stock has already been decremented in the database. Swallowing here means the
        // stock is gone and Payment is never told, so the order stalls holding reserved stock.

        // Arrange
        ThrowOnPublish();

        // Act
        var exception = Record.Exception(() => _sut.PublishStockReserved(4821, 250m, Items));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void PublishStockReservationFailed_WhenTheBrokerThrows_SwallowsTheException()
    {
        // BUG: the order is never told its reservation failed, so it stays Pending instead
        // of reaching OutOfStock.

        // Arrange
        ThrowOnPublish();

        // Act
        var exception = Record.Exception(() => _sut.PublishStockReservationFailed(4821, "reason"));

        // Assert
        Assert.Null(exception);
    }

    private void ThrowOnPublish() =>
        _rabbitMqPublisher
            .When(publisher => publisher.Publish(Arg.Any<object>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("broker unreachable"));
}
