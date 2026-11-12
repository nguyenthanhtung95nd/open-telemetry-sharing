using Inventory.Api.Common;
using Inventory.Api.Domain;
using Inventory.Api.Repositories;

namespace Inventory.Api.Services;

public sealed class InventoryService(IProductRepository repository) : IInventoryService
{
    public async Task<Result<InventoryAvailability>> CheckAvailabilityAsync(
        int productId, int quantity, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(productId, ct);
        if (product is null) return Result.Failure<InventoryAvailability>($"Product {productId} not found in inventory.");

        var isAvailable = product.AvailableQuantity >= quantity;
        return Result.Success(new InventoryAvailability(
            product.Id, quantity, isAvailable, product.AvailableQuantity, product.Price));
    }
}
