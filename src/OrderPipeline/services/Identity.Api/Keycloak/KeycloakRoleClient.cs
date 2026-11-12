using Identity.Api.Logging;
using Identity.Api.Common;
using Identity.Api.Keycloak.Models;

namespace Identity.Api.Keycloak;

public sealed class KeycloakRoleClient(KeycloakAdminHttp admin, ILogger<KeycloakRoleClient> logger)
    : IKeycloakRoleClient
{
    public async Task<Result<IReadOnlySet<Guid>>> GetMembersAsync(string roleName, CancellationToken ct = default)
    {
        try
        {
            var members = await admin.ReadAsync<List<KeycloakUser>>(
                $"/admin/realms/{admin.Realm}/roles/{roleName}/users", ct) ?? [];

            return Result.Success<IReadOnlySet<Guid>>(members.Select(m => Guid.Parse(m.Id)).ToHashSet());
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<IReadOnlySet<Guid>>(ex, roleName);
        }
    }

    public async Task<Result<IReadOnlySet<string>>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var roles = await admin.ReadAsync<List<RealmRole>>(
                $"/admin/realms/{admin.Realm}/users/{userId}/role-mappings/realm", ct) ?? [];

            return Result.Success<IReadOnlySet<string>>(roles.Select(r => r.Name).ToHashSet());
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<IReadOnlySet<string>>(ex, $"user {userId}");
        }
    }

    public async Task<Result<Guid>> GrantAsync(
        Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default) =>
        await MapRolesAsync(HttpMethod.Post, userId, roleNames, ct);

    public async Task<Result<Guid>> RevokeAsync(
        Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default) =>
        await MapRolesAsync(HttpMethod.Delete, userId, roleNames, ct);

    private async Task<Result<Guid>> MapRolesAsync(
        HttpMethod method, Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken ct)
    {
        if (roleNames.Count == 0) return Result.Success(userId);

        try
        {
            var roles = new List<RealmRole>();
            foreach (var name in roleNames)
            {
                var role = await admin.ReadAsync<RealmRole>($"/admin/realms/{admin.Realm}/roles/{name}", ct);
                if (role is null) return Result.Failure<Guid>($"Role '{name}' does not exist in the realm.");
                roles.Add(role);
            }

            var response = await admin.SendAsync(
                method, $"/admin/realms/{admin.Realm}/users/{userId}/role-mappings/realm", roles, ct);

            response.EnsureSuccessStatusCode();
            return Result.Success(userId);
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<Guid>(ex, string.Join(", ", roleNames));
        }
    }

    private Result<T> Unavailable<T>(HttpRequestException exception, string roleNames)
    {
        logger.KeycloakUnreachable(exception, $"handling roles {roleNames}");
        return Result.Failure<T>("Identity provider is currently unavailable.");
    }
}
