using System.Diagnostics;
using OpenTelemetry;
using OrderPipeline.Api.Clients;
using OrderPipeline.Api.Common;
using OrderPipeline.Api.Diagnostics;
using OrderPipeline.Api.Diagnostics.Extensions;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Logging;
using OrderPipeline.Api.Messaging;
using OrderPipeline.Api.Repositories;
using OrderPipeline.Api.Security;
using Shared.Messaging;

namespace OrderPipeline.Api.Services;

public sealed class OrderService(
    IOrderRepository orderRepository,
    IInventoryClient inventoryClient,
    IOrderEventsPublisher orderEventsPublisher,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<Result<Order>> GetByIdAsync(int id, AuthenticatedUser user, CancellationToken ct = default)
    {
        if (id <= 0) return Result.Failure<Order>("Order ID must be greater than zero.");

        var order = await orderRepository.GetByIdAsync(id, ct);
        if (order is null || !string.Equals(order.CustomerEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            logger.OrderNotFound(id);
            return Result.Failure<Order>($"Order {id} not found.");
        }

        return Result.Success(order);
    }

    public async Task<Result<Order>> CreateAsync(CreateOrderRequest request, AuthenticatedUser user, CancellationToken ct = default)
    {
        using var timer = logger.BeginTimedOperation("Creating order");

        if (request.Items.Count == 0) return Result.Failure<Order>("Order must contain at least one item.");

        var itemsResult = await BuildOrderItemsAsync(request.Items, ct);
        if (!itemsResult.IsSuccess)
        {
            logger.OrderCreationFailed(itemsResult.Error!);
            return Result.Failure<Order>(itemsResult.Error!);
        }

        var order = new Order
        {
            CustomerName = user.Name,
            CustomerEmail = user.Email,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            Items = itemsResult.Value!,
            TotalAmount = itemsResult.Value!.Sum(i => i.UnitPrice * i.Quantity)
        };

        Activity.Current.EnrichWithOrder(order);

        await orderRepository.AddAsync(order, ct);

        // Carried in baggage so Inventory and Payment can tag their own spans with the order
        // they are working on. The order id is deliberate: baggage crosses the wire in a
        // plaintext header, so nothing identifying the customer may travel in it.
        Baggage.SetBaggage(OrderPipelineTags.OrderId, order.Id.ToString());

        orderEventsPublisher.PublishOrderCreated(order);

        OrderMetrics.RecordAccepted(order);

        Activity.Current?.AddEvent(
            new ActivityEvent(
                "OrderPersisted",
                tags: new ActivityTagsCollection(
                    new KeyValuePair<string, object?>[]
                    {
                        new(OrderPipelineTags.OrderId, order.Id),
                        new(OrderPipelineTags.OrderItemCount, order.Items.Count),
                        new(OrderPipelineTags.OrderTotalAmount, order.TotalAmount)
                    }
                )
            )
        );

        logger.OrderCreated(order.Id, order.Items.Count, order.TotalAmount);

        return Result.Success(order);
    }

    private async Task<Result<List<OrderItem>>> BuildOrderItemsAsync(
        List<CreateOrderItemRequest> requestedItems, CancellationToken ct)
    {
        var items = new List<OrderItem>();

        foreach (var requested in requestedItems)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("validate order item");
            activity?.SetTag(OrderPipelineTags.ProductId, requested.ProductId);
            activity?.SetTag(OrderPipelineTags.RequestedQuantity, requested.Quantity);

            // Inventory.Api owns the catalog, so one call settles existence, stock and price.
            var availability = await inventoryClient.CheckAvailabilityAsync(requested.ProductId, requested.Quantity, ct);
            if (!availability.IsSuccess) return Result.Failure<List<OrderItem>>(availability.Error!);
            if (!availability.Value!.IsAvailable)
                return Result.Failure<List<OrderItem>>($"Insufficient stock for product {requested.ProductId}.");

            items.Add(new OrderItem
            {
                ProductId = requested.ProductId,
                Quantity = requested.Quantity,
                UnitPrice = availability.Value.UnitPrice
            });
        }

        return Result.Success(items);
    }
}
