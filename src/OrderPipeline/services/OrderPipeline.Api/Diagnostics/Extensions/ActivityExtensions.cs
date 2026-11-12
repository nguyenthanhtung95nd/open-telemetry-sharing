using System.Diagnostics;
using OrderPipeline.Api.Domain;
using Shared.Messaging;

namespace OrderPipeline.Api.Diagnostics.Extensions;

/// <summary>Enriches spans with the order attributes shared across the saga.</summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Stamps the order identity, status and total onto <paramref name="activity"/>.
    /// </summary>
    /// <param name="activity">The span to enrich. A null span is ignored.</param>
    /// <param name="order">The order whose attributes are written.</param>
    /// <returns>The same <paramref name="activity"/>, for chaining.</returns>
    public static Activity? EnrichWithOrder(this Activity? activity, Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        activity?.SetTag(OrderPipelineTags.OrderId, order.Id);
        activity?.SetTag(OrderPipelineTags.OrderStatus, order.Status.ToString());
        activity?.SetTag(OrderPipelineTags.OrderTotalAmount, order.TotalAmount);
        return activity;
    }
}
