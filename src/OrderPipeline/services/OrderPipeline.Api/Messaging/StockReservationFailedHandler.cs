using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Diagnostics;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Logging;
using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class StockReservationFailedHandler(IServiceScopeFactory scopeFactory, ILogger<StockReservationFailedHandler> logger)
    : IEventHandler<StockReservationFailedEvent>
{
    public async Task HandleAsync(StockReservationFailedEvent @event)
    {
        Activity.Current?.SetTag(OrderPipelineTags.OrderId, @event.OrderId);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId);
        if (order is null)
        {
            logger.OrderUnknownForEvent(nameof(StockReservationFailedEvent), @event.OrderId);
            return;
        }

        order.Status = OrderStatus.OutOfStock;
        await context.SaveChangesAsync();

        OrderMetrics.RecordSettled(order);
        logger.OrderStatusChanged(order.Id, order.Status);
    }
}
