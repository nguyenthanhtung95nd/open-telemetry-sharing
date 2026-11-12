using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Payment.Api.Data;
using Payment.Api.Diagnostics;
using Payment.Api.Domain;
using Payment.Api.Logging;
using Payment.Api.Simulation;
using Shared.Messaging;

namespace Payment.Api.Messaging;

public sealed class StockReservedHandler(
    IServiceScopeFactory scopeFactory,
    IPaymentSimulator paymentSimulator,
    IPaymentEventsPublisher paymentEventsPublisher,
    IOptions<PaymentOptions> options,
    ILogger<StockReservedHandler> logger)
    : IEventHandler<StockReservedEvent>
{
    public async Task HandleAsync(StockReservedEvent @event)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("authorize payment");
        activity?.SetTag(OrderPipelineTags.OrderId, @event.OrderId);
        activity?.SetTag(OrderPipelineTags.OrderTotalAmount, @event.Amount);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settled = await FindSettledStatusAsync(context, @event.OrderId);
        if (settled is not null)
        {
            ReportDuplicate(activity, @event, settled.Value);
            return;
        }

        var delay = options.Value.ProcessingDelayMs;
        if (delay > 0) await Task.Delay(delay);

        var status = paymentSimulator.ShouldFail() ? PaymentStatus.Failed : PaymentStatus.Succeeded;
        var raced = await TrySettleAsync(context, @event, status);
        if (raced is not null)
        {
            ReportDuplicate(activity, @event, raced.Value);
            return;
        }

        ReportOutcome(activity, @event, status);
    }

    /// <summary>
    /// Writes the payment row for this order.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when this delivery settled the order, or the status stored by
    /// the delivery that won the race for the unique index.
    /// </returns>
    private static async Task<PaymentStatus?> TrySettleAsync(
        AppDbContext context, StockReservedEvent @event, PaymentStatus status)
    {
        context.Payments.Add(new PaymentTransaction
        {
            OrderId = @event.OrderId,
            Amount = @event.Amount,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        });

        try
        {
            await context.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException)
        {
            // The pre-check catches the ordinary redelivery. Reaching here means two
            // deliveries raced, and the unique index decided which charge counts.
            context.ChangeTracker.Clear();
            return await FindSettledStatusAsync(context, @event.OrderId)
                   ?? throw new InvalidOperationException(
                       $"Payment for order {@event.OrderId} was rejected by the database but no row exists.");
        }
    }

    private static async Task<PaymentStatus?> FindSettledStatusAsync(AppDbContext context, int orderId)
    {
        var existing = await context.Payments.AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.OrderId == orderId);

        return existing?.Status;
    }

    /// <summary>
    /// Republishes the stored outcome rather than returning quietly. A handler that
    /// committed the charge and then died before publishing would otherwise leave the
    /// customer charged and the order stuck at StockReserved for good.
    /// </summary>
    private void ReportDuplicate(Activity? activity, StockReservedEvent @event, PaymentStatus status)
    {
        logger.PaymentAlreadyProcessed(@event.OrderId, status);
        activity?.SetTag(OrderPipelineTags.Outcome, OrderPipelineTags.OutcomeDuplicate);
        RecordOutcome(OrderPipelineTags.OutcomeDuplicate);
        Publish(@event, status);
    }

    private void ReportOutcome(Activity? activity, StockReservedEvent @event, PaymentStatus status)
    {
        if (status == PaymentStatus.Failed)
        {
            logger.PaymentDeclined(@event.OrderId, @event.Amount);
            activity?.SetStatus(ActivityStatusCode.Error, "Payment declined");
            activity?.SetTag(OrderPipelineTags.Outcome, OrderPipelineTags.OutcomeFailed);
            RecordOutcome(OrderPipelineTags.OutcomeFailed);
        }
        else
        {
            logger.PaymentSucceeded(@event.OrderId, @event.Amount);
            activity?.SetTag(OrderPipelineTags.Outcome, OrderPipelineTags.OutcomeSucceeded);
            RecordOutcome(OrderPipelineTags.OutcomeSucceeded);
        }

        Publish(@event, status);
    }

    private void Publish(StockReservedEvent @event, PaymentStatus status)
    {
        if (status == PaymentStatus.Failed)
        {
            paymentEventsPublisher.PublishPaymentFailed(@event.OrderId, @event.Items);
            return;
        }

        paymentEventsPublisher.PublishPaymentSucceeded(@event.OrderId);
    }

    private static void RecordOutcome(string outcome) =>
        ApplicationDiagnostics.PaymentsProcessedCounter.Add(
            1, new KeyValuePair<string, object?>(OrderPipelineTags.Outcome, outcome));
}
