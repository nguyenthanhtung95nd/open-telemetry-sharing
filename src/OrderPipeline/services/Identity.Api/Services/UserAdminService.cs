using Identity.Api.Diagnostics;
using Identity.Api.Logging;
using Identity.Api.Common;
using Identity.Api.Keycloak;
using Shared.Security;

namespace Identity.Api.Services;

public sealed class UserAdminService(
    IKeycloakUserClient users,
    IKeycloakRoleClient roles,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    /// <summary>
    /// The roles this screen governs. Keycloak also carries built-ins such as
    /// <c>offline_access</c> that no administrator should be toggling here.
    /// </summary>
    private static readonly string[] ManagedRoles = [RealmRoles.Customer, RealmRoles.Admin];

    private const string SelfLockoutMessage =
        "You cannot do that to your own account — ask another administrator.";

    public async Task<Result<IReadOnlyList<AdminUserView>>> ListAsync(CancellationToken ct = default)
    {
        var accounts = await users.ListAsync(ct);
        if (!accounts.IsSuccess) return Result.Failure<IReadOnlyList<AdminUserView>>(accounts.Error!);

        // One membership lookup per managed role, rather than one role lookup per user:
        // the cost stays flat as the realm grows.
        var membership = new Dictionary<string, IReadOnlySet<Guid>>();
        foreach (var role in ManagedRoles)
        {
            var members = await roles.GetMembersAsync(role, ct);
            if (!members.IsSuccess) return Result.Failure<IReadOnlyList<AdminUserView>>(members.Error!);
            membership[role] = members.Value!;
        }

        var view = accounts.Value!
            .Select(a => new AdminUserView(
                a.Id, a.Email, a.Name, a.Enabled,
                ManagedRoles.Where(r => membership[r].Contains(a.Id)).ToList()))
            .ToList();

        return Result.Success<IReadOnlyList<AdminUserView>>(view);
    }

    public async Task<Result<Guid>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var requested = Normalise(request.Roles);
        var unknown = requested.Except(ManagedRoles).ToList();
        if (unknown.Count > 0) return Result.Failure<Guid>($"Unknown role: {string.Join(", ", unknown)}.");

        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("create account");

        var created = await users.CreateAsync(
            new CreateKeycloakUserRequest(request.Email, request.Password, request.Name), ct);
        if (!created.IsSuccess) return created;

        var granted = await roles.GrantAsync(created.Value, requested, ct);
        if (!granted.IsSuccess) return granted;

        activity?.SetTag(IdentityTags.UserId, created.Value);
        logger.AccountCreated(created.Value.ToString(), requested);

        return created;
    }

    public async Task<Result<Guid>> UpdateProfileAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default) =>
        await users.UpdateProfileAsync(userId, request.Email, request.Name, ct);

    public async Task<Result<Guid>> ResetPasswordAsync(
        Guid userId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var result = await users.ResetPasswordAsync(userId, request.Password, ct);
        if (result.IsSuccess) logger.PasswordReset(userId.ToString());
        return result;
    }

    public async Task<Result<Guid>> EnableAsync(Guid userId, CancellationToken ct = default) =>
        await users.SetEnabledAsync(userId, true, ct);

    public async Task<Result<Guid>> DisableAsync(Guid callerId, Guid userId, CancellationToken ct = default)
    {
        if (callerId == userId) return Result.Failure<Guid>(SelfLockoutMessage);
        return await users.SetEnabledAsync(userId, false, ct);
    }

    public async Task<Result<Guid>> SetRolesAsync(
        Guid callerId, Guid userId, SetRolesRequest request, CancellationToken ct = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("set account roles");
        activity?.SetTag(IdentityTags.UserId, userId);

        var requested = Normalise(request.Roles);

        var unknown = requested.Except(ManagedRoles).ToList();
        if (unknown.Count > 0) return Result.Failure<Guid>($"Unknown role: {string.Join(", ", unknown)}.");

        if (callerId == userId && !requested.Contains(RealmRoles.Admin))
            return Result.Failure<Guid>(SelfLockoutMessage);

        var current = await roles.GetUserRolesAsync(userId, ct);
        if (!current.IsSuccess) return Result.Failure<Guid>(current.Error!);

        var toGrant = requested.Except(current.Value!).ToList();
        var toRevoke = ManagedRoles.Where(r => current.Value!.Contains(r) && !requested.Contains(r)).ToList();

        var granted = await roles.GrantAsync(userId, toGrant, ct);
        if (!granted.IsSuccess) return granted;

        var revoked = await roles.RevokeAsync(userId, toRevoke, ct);
        if (!revoked.IsSuccess) return revoked;

        logger.RolesChanged(userId.ToString(), requested);

        return Result.Success(userId);
    }

    public async Task<Result<Guid>> DeleteAsync(Guid callerId, Guid userId, CancellationToken ct = default)
    {
        if (callerId == userId) return Result.Failure<Guid>(SelfLockoutMessage);

        var result = await users.DeleteAsync(userId, ct);
        if (result.IsSuccess) logger.AccountDeleted(userId.ToString());
        return result;
    }

    private static List<string> Normalise(IReadOnlyList<string>? requested) =>
        (requested ?? []).Select(r => r.Trim()).Where(r => r.Length > 0).Distinct().ToList();
}
