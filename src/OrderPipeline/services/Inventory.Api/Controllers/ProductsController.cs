using Inventory.Api.Logging;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Security;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(
    IProductCatalogService catalog,
    ILogger<ProductsController> logger) : ControllerBase
{
    private const string CreatedAction = "create";
    private const string UpdatedAction = "update";
    private const string DeletedAction = "delete";

    /// <summary>
    /// Lists the catalog. Each product carries its live available quantity, so callers
    /// never see a stock figure that has drifted from the one reservations run against.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Every product, ordered by ID. An empty list when the catalog is empty.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await catalog.GetAllAsync(ct));
    }

    /// <summary>Retrieves a single product by its unique identifier.</summary>
    /// <param name="id">The product ID. Must be greater than zero.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The product if found; <c>404 Not Found</c> otherwise.</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await catalog.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Adds a product to the catalog. It is buyable as soon as this returns.</summary>
    /// <param name="request">Name, price and opening stock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the stored product.</returns>
    [HttpPost]
    [Authorize(Roles = RealmRoles.Admin)]
    public async Task<IActionResult> Create(SaveProductRequest request, CancellationToken ct)
    {
        var result = await catalog.CreateAsync(request, ct);
        if (result.IsSuccess) LogCatalogueChange(CreatedAction, result.Value!.Id);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>Replaces a product's name, price and available stock.</summary>
    /// <param name="id">The product to change.</param>
    /// <param name="request">The new values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated product, or <c>404 Not Found</c>.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RealmRoles.Admin)]
    public async Task<IActionResult> Update(int id, SaveProductRequest request, CancellationToken ct)
    {
        var result = await catalog.UpdateAsync(id, request, ct);
        if (result.IsSuccess) LogCatalogueChange(UpdatedAction, id);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Removes a product from the catalog. Past orders are unaffected — they store their
    /// own <c>UnitPrice</c> snapshot and live in a different service's database.
    /// </summary>
    /// <param name="id">The product to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c>, or <c>404 Not Found</c>.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RealmRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await catalog.DeleteAsync(id, ct);
        if (result.IsSuccess) LogCatalogueChange(DeletedAction, id);

        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    // An audit record without an actor is not an audit record, and the actor only exists at
    // this boundary. The subject claim is used rather than a name, which would be PII.
    private void LogCatalogueChange(string action, int productId) =>
        logger.CatalogueChanged(action, productId, User.FindFirst("sub")?.Value ?? "unknown");
}
