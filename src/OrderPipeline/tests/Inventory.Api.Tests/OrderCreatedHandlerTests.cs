using Inventory.Api.Data;
using Inventory.Api.Domain;
using Inventory.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Messaging;
using Xunit;

namespace Inventory.Api.Tests;

public class OrderCreatedHandlerTests
{
    private readonly IInventoryEventsPublisher _eventsPublisher = Substitute.For<IInventoryEventsPublisher>();

    private static IServiceScopeFactory BuildScopeFactory(string databaseName, params Product[] seedStocks)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        using var seedScope = provider.CreateScope();
        var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Products.AddRange(seedStocks);
        context.SaveChanges();

        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task HandleAsync_WhenStockSufficient_DecrementsAndPublishesStockReserved()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(
            nameof(HandleAsync_WhenStockSufficient_DecrementsAndPublishesStockReserved),
            new Product { Id = 1, AvailableQuantity = 10 });
        var sut = new OrderCreatedHandler(scopeFactory, _eventsPublisher, NullLogger<OrderCreatedHandler>.Instance);
        var @event = new OrderCreatedEvent(42, "Test Customer", 99.99m, [new OrderLineItem(1, 3)]);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        _eventsPublisher.Received(1).PublishStockReserved(42, 99.99m, Arg.Any<IReadOnlyList<OrderLineItem>>());
        _eventsPublisher.DidNotReceive().PublishStockReservationFailed(Arg.Any<int>(), Arg.Any<string>());

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = await context.Products.FirstAsync(p => p.Id == 1);
        Assert.Equal(7, stock.AvailableQuantity);
    }

    [Fact]
    public async Task HandleAsync_WhenStockInsufficient_PublishesFailureAndDoesNotDecrement()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(
            nameof(HandleAsync_WhenStockInsufficient_PublishesFailureAndDoesNotDecrement),
            new Product { Id = 1, AvailableQuantity = 2 });
        var sut = new OrderCreatedHandler(scopeFactory, _eventsPublisher, NullLogger<OrderCreatedHandler>.Instance);
        var @event = new OrderCreatedEvent(42, "Test Customer", 99.99m, [new OrderLineItem(1, 5)]);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        _eventsPublisher.Received(1).PublishStockReservationFailed(42, Arg.Any<string>());
        _eventsPublisher.DidNotReceive().PublishStockReserved(Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<IReadOnlyList<OrderLineItem>>());

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = await context.Products.FirstAsync(p => p.Id == 1);
        Assert.Equal(2, stock.AvailableQuantity);
    }

    [Fact]
    public async Task HandleAsync_WhenProductHasNoStockRecord_PublishesFailure()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenProductHasNoStockRecord_PublishesFailure));
        var sut = new OrderCreatedHandler(scopeFactory, _eventsPublisher, NullLogger<OrderCreatedHandler>.Instance);
        var @event = new OrderCreatedEvent(42, "Test Customer", 99.99m, [new OrderLineItem(999, 1)]);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        _eventsPublisher.Received(1).PublishStockReservationFailed(42, Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WhenOneOfMultipleItemsInsufficient_FailsAtomicallyWithoutPartialDecrement()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(
            nameof(HandleAsync_WhenOneOfMultipleItemsInsufficient_FailsAtomicallyWithoutPartialDecrement),
            new Product { Id = 1, AvailableQuantity = 10 },
            new Product { Id = 2, AvailableQuantity = 1 });
        var sut = new OrderCreatedHandler(scopeFactory, _eventsPublisher, NullLogger<OrderCreatedHandler>.Instance);
        var @event = new OrderCreatedEvent(42, "Test Customer", 99.99m,
            [new OrderLineItem(1, 5), new OrderLineItem(2, 5)]);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        _eventsPublisher.Received(1).PublishStockReservationFailed(42, Arg.Any<string>());

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var untouchedStock = await context.Products.FirstAsync(p => p.Id == 1);
        Assert.Equal(10, untouchedStock.AvailableQuantity);
    }
}
