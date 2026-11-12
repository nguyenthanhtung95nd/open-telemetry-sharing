using Identity.Api.Common;

namespace Identity.Api.Keycloak;

public sealed record CreateKeycloakUserRequest(string Email, string Password, string Name);

public sealed record KeycloakAccount(Guid Id, string Email, string Name, bool Enabled);

public interface IKeycloakUserClient
{
    Task<Result<IReadOnlyList<KeycloakAccount>>> ListAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(CreateKeycloakUserRequest request, CancellationToken ct = default);
    Task<Result<Guid>> UpdateProfileAsync(Guid userId, string email, string name, CancellationToken ct = default);
    Task<Result<Guid>> SetEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default);
    Task<Result<Guid>> ResetPasswordAsync(Guid userId, string password, CancellationToken ct = default);
    Task<Result<Guid>> DeleteAsync(Guid userId, CancellationToken ct = default);
}
