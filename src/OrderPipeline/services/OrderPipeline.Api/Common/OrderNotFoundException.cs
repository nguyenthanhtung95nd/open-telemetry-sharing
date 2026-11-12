namespace OrderPipeline.Api.Common;

public sealed class OrderNotFoundException(int orderId)
    : Exception($"Order {orderId} not found.");
