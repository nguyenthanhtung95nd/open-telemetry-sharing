using OrderPipeline.Api.Logging;
using OrderPipeline.Api.Common;
using System.Net;

namespace OrderPipeline.Api.Clients;

public sealed class InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger) : IInventoryClient
{
    public async Task<Result<InventoryAvailability>> CheckAvailabilityAsync(
        int productId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/inventory/{productId}/available?quantity={quantity}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result.Failure<InventoryAvailability>($"Product {productId} not found in inventory.");

            response.EnsureSuccessStatusCode();

            var availability = await response.Content.ReadFromJsonAsync<InventoryAvailability>(ct);
            return Result.Success(availability!);
        }
        catch (HttpRequestException ex)
        {
            logger.InventoryUnreachable(ex, productId);
            return Result.Failure<InventoryAvailability>("Inventory service unavailable.");
        }
    }
}
