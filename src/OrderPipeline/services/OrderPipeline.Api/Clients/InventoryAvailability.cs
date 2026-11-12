namespace OrderPipeline.Api.Clients;

public record InventoryAvailability(
    int ProductId,
    int RequestedQuantity,
    bool IsAvailable,
    int AvailableQuantity,
    decimal UnitPrice);
