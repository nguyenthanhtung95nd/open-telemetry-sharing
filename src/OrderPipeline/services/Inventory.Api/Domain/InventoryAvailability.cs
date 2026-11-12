namespace Inventory.Api.Domain;

public record InventoryAvailability(
    int ProductId,
    int RequestedQuantity,
    bool IsAvailable,
    int AvailableQuantity,
    decimal UnitPrice);
