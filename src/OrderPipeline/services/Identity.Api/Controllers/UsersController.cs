using System.Security.Claims;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Security;

namespace Identity.Api.Controllers;

/// <summary>
/// Account administration, backed by the Keycloak Admin API.
/// </summary>
/// <remarks>
/// Every action here requires the <c>admin</c> realm role. Registration lives on
/// <see cref="IdentityController"/> and stays open to anonymous callers.
/// </remarks>
[ApiController]
[Route("identity/users")]
[Authorize(Roles = RealmRoles.Admin)]
public sealed class UsersController(IUserAdminService userAdmin) : ControllerBase
{
    /// <summary>Lists every account with the realm roles it holds.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All accounts, ordered by email.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await userAdmin.ListAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>Creates an account and grants it the requested roles.</summary>
    /// <param name="request">Email, name, temporary password and roles.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>201 Created</c> with the new account's id.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await userAdmin.CreateAsync(request, ct);
        return result.IsSuccess
            ? Created(string.Empty, new { userId = result.Value })
            : BadRequest(result.Error);
    }

    /// <summary>Changes an account's email and display name.</summary>
    /// <param name="id">The account to change.</param>
    /// <param name="request">The new email and name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, UpdateProfileRequest request, CancellationToken ct) =>
        Respond(await userAdmin.UpdateProfileAsync(id, request, ct));

    /// <summary>Sets a new password on an account.</summary>
    /// <param name="id">The account to change.</param>
    /// <param name="request">The new password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken ct) =>
        Respond(await userAdmin.ResetPasswordAsync(id, request, ct));

    /// <summary>Lets an account sign in again.</summary>
    /// <param name="id">The account to enable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct) =>
        Respond(await userAdmin.EnableAsync(id, ct));

    /// <summary>Stops an account from signing in. Refused on the caller's own account.</summary>
    /// <param name="id">The account to disable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct) =>
        Respond(await userAdmin.DisableAsync(CallerId, id, ct));

    /// <summary>
    /// Replaces an account's realm roles with the given set. Refused if it would strip
    /// the caller's own <c>admin</c> role.
    /// </summary>
    /// <param name="id">The account to change.</param>
    /// <param name="request">The complete set of roles the account should end up with.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, SetRolesRequest request, CancellationToken ct) =>
        Respond(await userAdmin.SetRolesAsync(CallerId, id, request, ct));

    /// <summary>Removes an account. Refused on the caller's own account.</summary>
    /// <param name="id">The account to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        Respond(await userAdmin.DeleteAsync(CallerId, id, ct));

    private IActionResult Respond(Common.Result<Guid> result) =>
        result.IsSuccess ? NoContent() : BadRequest(result.Error);

    private Guid CallerId =>
        Guid.TryParse(User.FindFirstValue(KeycloakClaimTypes.Subject), out var id) ? id : Guid.Empty;
}
