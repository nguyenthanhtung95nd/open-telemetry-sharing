using Payment.Api.Domain;

namespace Payment.Api.Logging;

/// <summary>
/// Event ids for this service. The 4000 range belongs to Payment.Api.
/// </summary>
public static class LogEventIds
{
    /// <summary>A payment was authorised.</summary>
    public const int PaymentSucceeded = 4001;

    /// <summary>A payment was declined.</summary>
    public const int PaymentFailed = 4002;

    /// <summary>An integration event could not be published.</summary>
    public const int PaymentEventPublishFailed = 4003;

    /// <summary>A delivery arrived for an order that had already been charged.</summary>
    public const int PaymentAlreadyProcessed = 4004;

    /// <summary>Events that must always reach stdout, whatever the configured log levels.</summary>
    public static readonly int[] Audited =
        [PaymentSucceeded, PaymentFailed, PaymentAlreadyProcessed];
}

/// <summary>
/// Every log message Payment.Api emits.
/// </summary>
/// <remarks>
/// No message here may carry personally identifiable information or card data.
/// </remarks>
public static partial class PaymentLog
{
    /// <summary>Logs that a payment was authorised.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order that was paid.</param>
    /// <param name="amount">Amount authorised.</param>
    [LoggerMessage(
        EventId = LogEventIds.PaymentSucceeded,
        Level = LogLevel.Information,
        Message = "Authorised payment of {Amount} for order {OrderId}")]
    public static partial void PaymentSucceeded(this ILogger logger, int orderId, decimal amount);

    /// <summary>Logs that a payment was declined.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order whose payment was declined.</param>
    /// <param name="amount">Amount that was attempted.</param>
    [LoggerMessage(
        EventId = LogEventIds.PaymentFailed,
        Level = LogLevel.Warning,
        Message = "Declined payment of {Amount} for order {OrderId}")]
    public static partial void PaymentDeclined(this ILogger logger, int orderId, decimal amount);

    /// <summary>Logs that a redelivery arrived for an order already charged.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="orderId">Order that already has a payment row.</param>
    /// <param name="status">Outcome stored by the delivery that got there first.</param>
    [LoggerMessage(
        EventId = LogEventIds.PaymentAlreadyProcessed,
        Level = LogLevel.Information,
        Message = "Order {OrderId} was already {Status}, republishing that outcome instead of charging again")]
    public static partial void PaymentAlreadyProcessed(
        this ILogger logger, int orderId, PaymentStatus status);

    /// <summary>Logs that an integration event could not be published.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The failure reported by the broker.</param>
    /// <param name="eventType">Name of the event contract being published.</param>
    /// <param name="orderId">Order the event belonged to.</param>
    [LoggerMessage(
        EventId = LogEventIds.PaymentEventPublishFailed,
        Level = LogLevel.Error,
        Message = "Could not publish {EventType} for order {OrderId}. The saga stalls until it is replayed.")]
    public static partial void PaymentEventPublishFailed(
        this ILogger logger, Exception exception, string eventType, int orderId);
}
