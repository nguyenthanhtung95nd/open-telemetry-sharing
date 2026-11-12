namespace Shared.Messaging;

/// <summary>
/// The span and metric attribute names shared by every service in the order saga. They live
/// beside the event contracts because a tag that only one service spells correctly is a tag
/// nobody can query across the saga.
/// </summary>
/// <remarks>
/// No attribute here may carry personally identifiable information. These values travel in
/// baggage across service and broker boundaries and are stored by the telemetry backend.
/// </remarks>
public static class OrderPipelineTags
{
    /// <summary>Identifier of the order a span or measurement belongs to.</summary>
    public const string OrderId = "order.id";

    /// <summary>Current status of the order, as the <c>OrderStatus</c> name.</summary>
    public const string OrderStatus = "order.status";

    /// <summary>Monetary total of the order.</summary>
    public const string OrderTotalAmount = "order.total_amount";

    /// <summary>Number of line items on the order.</summary>
    public const string OrderItemCount = "order.item_count";

    /// <summary>Identifier of the product a line item refers to.</summary>
    public const string ProductId = "product.id";

    /// <summary>Quantity asked for by a line item.</summary>
    public const string RequestedQuantity = "requested.quantity";

    /// <summary>Why a stock reservation or a payment could not complete.</summary>
    public const string FailureReason = "failure.reason";

    /// <summary>How a saga step ended, used to partition counters.</summary>
    public const string Outcome = "outcome";

    /// <summary>Value of <see cref="Outcome"/> for a step that completed as intended.</summary>
    public const string OutcomeSucceeded = "succeeded";

    /// <summary>Value of <see cref="Outcome"/> for a step that was rejected.</summary>
    public const string OutcomeFailed = "failed";

    /// <summary>
    /// Value of <see cref="Outcome"/> for a step that had already run for this order and
    /// was skipped. Distinguishing it from succeeded keeps the funnel honest.
    /// </summary>
    public const string OutcomeDuplicate = "duplicate";

    /// <summary>Baggage key prefix the services allow onto spans.</summary>
    public const string BaggagePrefix = "order.";
}
