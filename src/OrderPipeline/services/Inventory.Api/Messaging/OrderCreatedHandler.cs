using System.Diagnostics;
using Inventory.Api.Data;
using Inventory.Api.Diagnostics;
using Inventory.Api.Domain;
using Inventory.Api.Logging;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;

namespace Inventory.Api.Messaging;

public sealed class OrderCreatedHandler(
    IServiceScopeFactory scopeFactory,
    IInventoryEventsPublisher inventoryEventsPublisher,
    ILogger<OrderCreatedHandler> logger)
    : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("reserve stock");
        activity?.SetTag(OrderPipelineTags.OrderId, @event.OrderId);
        activity?.SetTag(OrderPipelineTags.OrderItemCount, @event.Items.Count);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await IsAlreadyReservedAsync(context, @event.OrderId))
        {
            ReportDuplicate(activity, @event);
            return;
        }

        var stocks = await LoadStocksAsync(context, @event.Items);
        var shortfallReason = FindInsufficientStock(stocks, @event.Items);
        if (shortfallReason is not null)
        {
            ReportShortfall(activity, @event, shortfallReason);
            return;
        }

        if (!await TryReserveAsync(context, stocks, @event))
        {
            ReportDuplicate(activity, @event);
            return;
        }

        activity?.SetTag(OrderPipelineTags.Outcome, OrderPipelineTags.OutcomeSucceeded);
        RecordOutcome(OrderPipelineTags.OutcomeSucceeded);
        logger.StockReserved(@event.OrderId, @event.Items.Count);
        inventoryEventsPublisher.PublishStockReserved(@event.OrderId, @event.TotalAmount, @event.Items);
    }

    /// <summary>
    /// Writes the reservation row and the stock decrements in one <c>SaveChanges</c>, so a
    /// duplicate order id rolls back the decrement along with the insert.
    /// </summary>
    /// <returns><see langword="false"/> when another delivery already reserved this order.</returns>
    private static async Task<bool> TryReserveAsync(
        AppDbContext context, Dictionary<int, Product> stocks, OrderCreatedEvent @event)
    {
        context.StockReservations.Add(
            new StockReservation { OrderId = @event.OrderId, ReservedAtUtc = DateTime.UtcNow });

        foreach (var item in @event.Items)
        {
            stocks[item.ProductId].AvailableQuantity -= item.Quantity;
        }

        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // The pre-check above catches the ordinary redelivery. Reaching here means two
            // deliveries raced and the primary key decided the winner, which is the only
            // place that decision can safely be made.
            context.ChangeTracker.Clear();
            return false;
        }
    }

    private static Task<bool> IsAlreadyReservedAsync(AppDbContext context, int orderId) =>
        context.StockReservations.AsNoTracking().AnyAsync(r => r.OrderId == orderId);

    /// <summary>
    /// Republishes rather than returning quietly. A handler that committed its reservation
    /// and then died before publishing would otherwise leave the saga stopped forever, and
    /// the stock already taken. Downstream is idempotent, so a repeat is harmless.
    /// </summary>
    private void ReportDuplicate(Activity? activity, OrderCreatedEvent @event)
    {
        logger.StockAlreadyReserved(@event.OrderId);
        activity?.SetTag(OrderPipelineTags.Outcome, OrderPipelineTags.OutcomeDuplicate);
        RecordOutcome(OrderPipelineTags.OutcomeDuplicate);
        inventoryEventsPublisher.PublishStockReserved(@event.OrderId, @event.TotalAmount, @event.Items);
    }

    private void ReportShortfall(Activity? activity, OrderCreatedEvent @event, string reason)
    {
        logger.StockReservationFailed(@event.OrderId, reason);
        activity?.SetTag(OrderPipelineTags.Outcome, OrderPipelineTags.OutcomeFailed);
        activity?.SetTag(OrderPipelineTags.FailureReason, reason);
        RecordOutcome(OrderPipelineTags.OutcomeFailed);
        inventoryEventsPublisher.PublishStockReservationFailed(@event.OrderId, reason);
    }

    private static void RecordOutcome(string outcome) =>
        ApplicationDiagnostics.StockReservationsCounter.Add(
            1, new KeyValuePair<string, object?>(OrderPipelineTags.Outcome, outcome));

    private static async Task<Dictionary<int, Product>> LoadStocksAsync(
        AppDbContext context, IReadOnlyList<OrderLineItem> items)
    {
        var productIds = items.Select(i => i.ProductId).ToList();
        return await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
    }

    private static string? FindInsufficientStock(
        Dictionary<int, Product> stocks, IReadOnlyList<OrderLineItem> items)
    {
        foreach (var item in items)
        {
            if (!stocks.TryGetValue(item.ProductId, out var stock))
                return $"Product {item.ProductId} has no stock record.";

            if (stock.AvailableQuantity < item.Quantity)
                return $"Product {item.ProductId} has only {stock.AvailableQuantity} available, requested {item.Quantity}.";
        }

        return null;
    }
}
