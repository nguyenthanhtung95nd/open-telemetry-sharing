using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Messaging;
using Shared.Messaging;
using Xunit;

namespace OrderPipeline.Api.Tests;

public class StockReservedHandlerTests
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

    private static Order OrderWith(OrderStatus status) => new()
    {
        Id = 1,
        CustomerName = "Test",
        CustomerEmail = "test@example.com",
        Status = status
    };

    private static StockReservedEvent Reserved() =>
        new(1, 49.99m, [new OrderLineItem(1, 1)]);

    private static async Task<OrderStatus> StatusAfterAsync(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await context.Orders.FirstAsync(o => o.Id == 1)).Status;
    }

    [Fact]
    public async Task HandleAsync_WhenOrderIsStillPending_MarksItStockReserved()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(
            nameof(HandleAsync_WhenOrderIsStillPending_MarksItStockReserved),
            OrderWith(OrderStatus.Pending));
        var sut = new StockReservedHandler(scopeFactory, NullLogger<StockReservedHandler>.Instance);

        // Act
        await sut.HandleAsync(Reserved());

        // Assert
        Assert.Equal(OrderStatus.StockReserved, await StatusAfterAsync(scopeFactory));
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.PaymentFailed)]
    [InlineData(OrderStatus.OutOfStock)]
    public async Task HandleAsync_WhenPaymentAlreadySettledTheOrder_LeavesItAlone(OrderStatus settled)
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(
            $"{nameof(HandleAsync_WhenPaymentAlreadySettledTheOrder_LeavesItAlone)}-{settled}",
            OrderWith(settled));
        var sut = new StockReservedHandler(scopeFactory, NullLogger<StockReservedHandler>.Instance);

        // Act
        await sut.HandleAsync(Reserved());

        // Assert
        Assert.Equal(settled, await StatusAfterAsync(scopeFactory));
    }

    [Fact]
    public async Task HandleAsync_WhenOrderIsUnknown_DoesNothing()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenOrderIsUnknown_DoesNothing));
        var sut = new StockReservedHandler(scopeFactory, NullLogger<StockReservedHandler>.Instance);

        // Act
        await sut.HandleAsync(new StockReservedEvent(999, 10m, [new OrderLineItem(1, 1)]));

        // Assert
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(context.Orders);
    }
}
