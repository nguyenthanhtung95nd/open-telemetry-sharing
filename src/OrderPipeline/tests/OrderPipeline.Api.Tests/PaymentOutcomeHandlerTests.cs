using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Messaging;
using Shared.Messaging;
using Xunit;

namespace OrderPipeline.Api.Tests;

public class PaymentOutcomeHandlerTests
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
    public async Task PaymentSucceededHandler_MarksOrderPaid()
    {
        // Arrange
        var order = new Order { Id = 1, CustomerName = "Test", CustomerEmail = "test@example.com", Status = OrderStatus.Pending };
        var scopeFactory = BuildScopeFactory(nameof(PaymentSucceededHandler_MarksOrderPaid), order);
        var sut = new PaymentSucceededHandler(scopeFactory, NullLogger<PaymentSucceededHandler>.Instance);

        // Act
        await sut.HandleAsync(new PaymentSucceededEvent(1));

        // Assert
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await context.Orders.FirstAsync(o => o.Id == 1);
        Assert.Equal(OrderStatus.Paid, updated.Status);
    }

    [Fact]
    public async Task PaymentFailedHandler_MarksOrderPaymentFailed()
    {
        // Arrange
        var order = new Order { Id = 1, CustomerName = "Test", CustomerEmail = "test@example.com", Status = OrderStatus.Pending };
        var scopeFactory = BuildScopeFactory(nameof(PaymentFailedHandler_MarksOrderPaymentFailed), order);
        var sut = new PaymentFailedHandler(scopeFactory, NullLogger<PaymentFailedHandler>.Instance);

        // Act
        await sut.HandleAsync(new PaymentFailedEvent(1, [new OrderLineItem(1, 1)]));

        // Assert
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await context.Orders.FirstAsync(o => o.Id == 1);
        Assert.Equal(OrderStatus.PaymentFailed, updated.Status);
    }
}
