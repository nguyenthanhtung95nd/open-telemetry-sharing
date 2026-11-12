using System.Diagnostics;
using Inventory.Api.Logging;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Messaging;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("inventory")]
public sealed class InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger) : ControllerBase
{
    /// <summary>
    /// Reports whether a product can satisfy the requested quantity.
    /// </summary>
    /// <param name="productId">Product to check.</param>
    /// <param name="quantity">Quantity the caller wants.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the availability, or 404 when the product is unknown.</returns>
    [HttpGet("{productId:int}/available")]
    public async Task<IActionResult> GetAvailability(int productId, [FromQuery] int quantity, CancellationToken ct)
    {
        // This call happens while the order is still being assembled, so no order id exists
        // yet to correlate on. The trace context already ties this span to the caller, and
        // anything identifying the customer must never travel in baggage.
        Activity.Current?.SetTag(OrderPipelineTags.ProductId, productId);
        Activity.Current?.SetTag(OrderPipelineTags.RequestedQuantity, quantity);

        logger.AvailabilityChecked(productId, quantity);

        var result = await inventoryService.CheckAvailabilityAsync(productId, quantity, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
