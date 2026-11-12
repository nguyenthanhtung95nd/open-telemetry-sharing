using Inventory.Api.Data;
using Inventory.Api.Domain;
using Inventory.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Messaging;
using Xunit;

namespace Inventory.Api.Tests;

public class PaymentFailedHandlerTests
{
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
    public async Task HandleAsync_RestoresPreviouslyReservedQuantity()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(
            nameof(HandleAsync_RestoresPreviouslyReservedQuantity),
            new Product { Id = 1, AvailableQuantity = 18 });
        var sut = new PaymentFailedHandler(scopeFactory, NullLogger<PaymentFailedHandler>.Instance);
        var @event = new PaymentFailedEvent(42, [new OrderLineItem(1, 30)]);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = await context.Products.FirstAsync(p => p.Id == 1);
        Assert.Equal(48, stock.AvailableQuantity);
    }

    [Fact]
    public async Task HandleAsync_WhenProductUnknown_DoesNotThrow()
    {
        // Arrange
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenProductUnknown_DoesNotThrow));
        var sut = new PaymentFailedHandler(scopeFactory, NullLogger<PaymentFailedHandler>.Instance);
        var @event = new PaymentFailedEvent(42, [new OrderLineItem(999, 5)]);

        // Act & Assert
        await sut.HandleAsync(@event);
    }
}
