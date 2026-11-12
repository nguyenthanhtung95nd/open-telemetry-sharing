namespace OrderPipeline.Api.Common;

public sealed class InsufficientStockException(int productId, int requested, int available)
    : Exception($"Product {productId} has insufficient stock: requested {requested}, available {available}.");
