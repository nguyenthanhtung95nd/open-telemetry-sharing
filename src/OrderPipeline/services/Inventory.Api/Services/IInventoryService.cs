using Inventory.Api.Common;
using Inventory.Api.Domain;

namespace Inventory.Api.Services;

public interface IInventoryService
{
    Task<Result<InventoryAvailability>> CheckAvailabilityAsync(int productId, int quantity, CancellationToken ct = default);
}
