using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Logging;
using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class StockReservedHandler(IServiceScopeFactory scopeFactory, ILogger<StockReservedHandler> logger)
    : IEventHandler<StockReservedEvent>
{
    public async Task HandleAsync(StockReservedEvent @event)
    {
        Activity.Current?.SetTag(OrderPipelineTags.OrderId, @event.OrderId);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId);
        if (order is null)
        {
            logger.OrderUnknownForEvent(nameof(StockReservedEvent), @event.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.OrderStatusChangeIgnored(nameof(StockReservedEvent), @event.OrderId, order.Status);
            return;
        }

        order.Status = OrderStatus.StockReserved;
        await context.SaveChangesAsync();

        logger.OrderStatusChanged(order.Id, order.Status);
    }
}
