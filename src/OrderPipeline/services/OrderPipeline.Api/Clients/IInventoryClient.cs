using OrderPipeline.Api.Common;

namespace OrderPipeline.Api.Clients;

public interface IInventoryClient
{
    Task<Result<InventoryAvailability>> CheckAvailabilityAsync(int productId, int quantity, CancellationToken ct = default);
}
