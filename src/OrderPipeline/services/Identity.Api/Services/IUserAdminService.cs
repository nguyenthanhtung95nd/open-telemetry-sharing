using Identity.Api.Common;

namespace Identity.Api.Services;

/// <summary>
/// Administration of accounts and their realm roles.
/// </summary>
/// <remarks>
/// Methods that could lock the signed-in administrator out of the system take the
/// caller's own id so they can refuse. Enable and disable are separate methods rather
/// than one taking a flag, because only one of the two is dangerous.
/// </remarks>
public interface IUserAdminService
{
    Task<Result<IReadOnlyList<AdminUserView>>> ListAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<Guid>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<Result<Guid>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken ct = default);
    Task<Result<Guid>> EnableAsync(Guid userId, CancellationToken ct = default);
    Task<Result<Guid>> DisableAsync(Guid callerId, Guid userId, CancellationToken ct = default);
    Task<Result<Guid>> SetRolesAsync(Guid callerId, Guid userId, SetRolesRequest request, CancellationToken ct = default);
    Task<Result<Guid>> DeleteAsync(Guid callerId, Guid userId, CancellationToken ct = default);
}
