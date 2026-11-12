using Inventory.Api.Logging;
using Inventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;

namespace Inventory.Api.Messaging;

public sealed class PaymentFailedHandler(IServiceScopeFactory scopeFactory, ILogger<PaymentFailedHandler> logger)
    : IEventHandler<PaymentFailedEvent>
{
    public async Task HandleAsync(PaymentFailedEvent @event)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productIds = @event.Items.Select(i => i.ProductId).ToList();
        var stocks = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in @event.Items)
        {
            if (!stocks.TryGetValue(item.ProductId, out var stock))
            {
                logger.CompensationProductUnknown(item.ProductId, @event.OrderId);
                continue;
            }

            stock.AvailableQuantity += item.Quantity;
        }

        await context.SaveChangesAsync();

        logger.StockCompensated(@event.OrderId);
    }
}
