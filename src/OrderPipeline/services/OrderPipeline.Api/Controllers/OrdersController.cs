using OrderPipeline.Api.Logging;
using OrderPipeline.Api.Security;
using OrderPipeline.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderPipeline.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class OrdersController(IOrderService orderService, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        logger.RequestReceived(nameof(GetById));

        var result = await orderService.GetByIdAsync(id, GetAuthenticatedUser(), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var user = GetAuthenticatedUser();
        logger.RequestReceived(nameof(Create));

        var result = await orderService.CreateAsync(request, user, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Error);
    }

    private AuthenticatedUser GetAuthenticatedUser() => new(
        GetClaimValue("sub"),
        GetClaimValue("email"),
        GetClaimValue("name"));

    private string GetClaimValue(string claimType) =>
        User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value ?? string.Empty;
}
