namespace OrderPipeline.Api.Services;

public sealed record CreateOrderRequest(List<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(int ProductId, int Quantity);
