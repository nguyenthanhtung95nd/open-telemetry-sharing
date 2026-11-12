using Inventory.Api.Data;
using Inventory.Api.Domain;
using Inventory.Api.Logging;
using Inventory.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.Messaging;
using Xunit;

namespace Inventory.Api.Tests;

/// <summary>
/// Phase 4 of the plan switches the consumer to at-least-once delivery, after which the
/// same event can arrive more than once. These assert the guard that has to be in place
/// before that happens, because a redelivery would otherwise decrement stock twice.
/// </summary>
public class OrderCreatedHandlerIdempotencyTests
{
    private const int OrderId = 42;
    private const int ProductId = 1;

    private readonly IInventoryEventsPublisher _eventsPublisher = Substitute.For<IInventoryEventsPublisher>();
    private readonly RecordingLogger<OrderCreatedHandler> _logger = new();

    private static readonly OrderCreatedEvent Event =
        new(OrderId, "Test Customer", 99.99m, [new OrderLineItem(ProductId, 3)]);

    [Fact]
    public async Task HandleAsync_CalledTwice_DecrementsStockOnce()
    {
        // Arrange
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_DecrementsStockOnce), openingStock: 10);
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        Assert.Equal(7, ReadStock(provider));
    }

    [Fact]
    public async Task HandleAsync_CalledTwice_WritesOneReservation()
    {
        // Arrange
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_WritesOneReservation), openingStock: 10);
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        Assert.Single(ReadReservations(provider), reservation => reservation.OrderId == OrderId);
    }

    [Fact]
    public async Task HandleAsync_CalledTwice_RepublishesStockReserved()
    {
        // A handler that committed its reservation and then died before publishing must
        // still be able to move the saga on. Returning quietly would strand the order.

        // Arrange
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_RepublishesStockReserved), openingStock: 10);
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        _eventsPublisher.Received(2).PublishStockReserved(
            OrderId, Event.TotalAmount, Arg.Any<IReadOnlyList<OrderLineItem>>());
    }

    [Fact]
    public async Task HandleAsync_OnTheDuplicate_LogsAtInformationWithItsOwnEventId()
    {
        // Arrange
        var provider = BuildProvider(
            nameof(HandleAsync_OnTheDuplicate_LogsAtInformationWithItsOwnEventId), openingStock: 10);
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        var duplicate = Assert.Single(
            _logger.Entries, entry => entry.EventId == LogEventIds.StockAlreadyReserved);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, duplicate.Level);
    }

    [Fact]
    public async Task HandleAsync_RecordsWhenTheStockWasTaken()
    {
        // The table exists to answer which orders are holding stock, not only to deduplicate.

        // Arrange
        var provider = BuildProvider(nameof(HandleAsync_RecordsWhenTheStockWasTaken), openingStock: 10);
        var before = DateTime.UtcNow;

        // Act
        await BuildHandler(provider).HandleAsync(Event);

        // Assert
        var reservation = Assert.Single(ReadReservations(provider));
        Assert.InRange(reservation.ReservedAtUtc, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_WhenStockIsShort_WritesNoReservation()
    {
        // A refused order holds nothing, so a later retry must be free to try again.

        // Arrange
        var provider = BuildProvider(nameof(HandleAsync_WhenStockIsShort_WritesNoReservation), openingStock: 1);

        // Act
        await BuildHandler(provider).HandleAsync(Event);

        // Assert
        Assert.Empty(ReadReservations(provider));
    }

    private OrderCreatedHandler BuildHandler(ServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), _eventsPublisher, _logger);

    private static ServiceProvider BuildProvider(string databaseName, int openingStock)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Products.Add(new Product { Id = ProductId, AvailableQuantity = openingStock });
        context.SaveChanges();

        return provider;
    }

    private static int ReadStock(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Products.AsNoTracking().Single(product => product.Id == ProductId).AvailableQuantity;
    }

    private static List<StockReservation> ReadReservations(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .StockReservations.AsNoTracking().ToList();
    }
}
