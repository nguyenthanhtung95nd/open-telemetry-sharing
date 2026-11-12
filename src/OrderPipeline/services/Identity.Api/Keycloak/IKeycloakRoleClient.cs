using Identity.Api.Common;

namespace Identity.Api.Keycloak;

public interface IKeycloakRoleClient
{
    Task<Result<IReadOnlySet<Guid>>> GetMembersAsync(string roleName, CancellationToken ct = default);

    Task<Result<IReadOnlySet<string>>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);

    Task<Result<Guid>> GrantAsync(Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default);

    Task<Result<Guid>> RevokeAsync(Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default);
}
