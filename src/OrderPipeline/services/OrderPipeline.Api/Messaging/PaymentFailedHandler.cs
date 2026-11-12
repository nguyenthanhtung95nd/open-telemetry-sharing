using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Diagnostics;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Logging;
using Shared.Messaging;

namespace OrderPipeline.Api.Messaging;

public sealed class PaymentFailedHandler(IServiceScopeFactory scopeFactory, ILogger<PaymentFailedHandler> logger)
    : IEventHandler<PaymentFailedEvent>
{
    public async Task HandleAsync(PaymentFailedEvent @event)
    {
        Activity.Current?.SetTag(OrderPipelineTags.OrderId, @event.OrderId);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId);
        if (order is null)
        {
            logger.OrderUnknownForEvent(nameof(PaymentFailedEvent), @event.OrderId);
            return;
        }

        order.Status = OrderStatus.PaymentFailed;
        await context.SaveChangesAsync();

        OrderMetrics.RecordSettled(order);
        logger.OrderStatusChanged(order.Id, order.Status);
    }
}
