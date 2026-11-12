using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Messaging;
using Shared.Messaging;
using Xunit;

namespace OrderPipeline.Api.Tests;

public class StockReservationFailedHandlerTests
{
    private static IServiceScopeFactory BuildScopeFactory(string databaseName, params Order[] seedOrders)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        using var seedScope = provider.CreateScope();
        var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Orders.AddRange(seedOrders);
        context.SaveChanges();

        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task HandleAsync_WhenOrderExists_MarksOrderOutOfStock()
    {
        // Arrange
        var order = new Order { Id = 1, CustomerName = "Test", CustomerEmail = "test@example.com", Status = OrderStatus.Pending };
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenOrderExists_MarksOrderOutOfStock), order);
        var sut = new StockReservationFailedHandler(scopeFactory, NullLogger<StockReservationFailedHandler>.Instance);

        // Act
        await sut.HandleAsync(new StockReservationFailedEvent(1, "Insufficient stock"));

        // Assert
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await context.Orders.FirstAsync(o => o.Id == 1);
        Assert.Equal(OrderStatus.OutOfStock, updated.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenOrderDoesNotExist_DoesNotThrow));
        var sut = new StockReservationFailedHandler(scopeFactory, NullLogger<StockReservationFailedHandler>.Instance);

        // Act & Assert
        await sut.HandleAsync(new StockReservationFailedEvent(999, "Insufficient stock"));
    }
}
