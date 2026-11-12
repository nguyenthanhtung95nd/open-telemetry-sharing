using Identity.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("identity")]
public sealed class IdentityController(IRegistrationService registrationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken ct)
    {
        var result = await registrationService.RegisterAsync(request, ct);
        return result.IsSuccess
            ? Created(string.Empty, new { userId = result.Value })
            : BadRequest(result.Error);
    }
}
