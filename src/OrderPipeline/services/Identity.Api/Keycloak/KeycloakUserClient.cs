using Identity.Api.Logging;
using System.Net;
using Identity.Api.Common;
using Identity.Api.Keycloak.Models;

namespace Identity.Api.Keycloak;

public sealed class KeycloakUserClient(KeycloakAdminHttp admin, ILogger<KeycloakUserClient> logger)
    : IKeycloakUserClient
{
    private const int MaxUsersListed = 200;

    public async Task<Result<IReadOnlyList<KeycloakAccount>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var users = await admin.ReadAsync<List<KeycloakUser>>(
                $"/admin/realms/{admin.Realm}/users?max={MaxUsersListed}", ct) ?? [];

            return Result.Success<IReadOnlyList<KeycloakAccount>>(
                users.Select(ToAccount).OrderBy(a => a.Email).ToList());
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<IReadOnlyList<KeycloakAccount>>(ex, "listing users");
        }
    }

    public async Task<Result<Guid>> CreateAsync(CreateKeycloakUserRequest request, CancellationToken ct = default)
    {
        try
        {
            var (firstName, lastName) = SplitName(request.Name);
            var payload = new CreateUserPayload(
                Username: request.Email,
                Email: request.Email,
                FirstName: firstName,
                LastName: lastName,
                Enabled: true,
                EmailVerified: true,
                Credentials: [new KeycloakCredential("password", request.Password, false)]);

            var response = await admin.SendAsync(HttpMethod.Post, $"/admin/realms/{admin.Realm}/users", payload, ct);

            if (response.StatusCode == HttpStatusCode.Conflict)
                return Result.Failure<Guid>("This email is already registered.");

            response.EnsureSuccessStatusCode();
            return Result.Success(Guid.Parse(response.Headers.Location!.Segments[^1]));
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<Guid>(ex, "creating a user");
        }
    }

    public async Task<Result<Guid>> UpdateProfileAsync(
        Guid userId, string email, string name, CancellationToken ct = default)
    {
        var (firstName, lastName) = SplitName(name);
        return await UpdateAsync(userId, new UpdateUserPayload(email, firstName, lastName), "updating a user", ct);
    }

    public async Task<Result<Guid>> SetEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default)
    {
        return await UpdateAsync(userId, new UpdateUserPayload(Enabled: enabled), "enabling a user", ct);
    }

    public async Task<Result<Guid>> ResetPasswordAsync(Guid userId, string password, CancellationToken ct = default)
    {
        try
        {
            var response = await admin.SendAsync(
                HttpMethod.Put,
                $"/admin/realms/{admin.Realm}/users/{userId}/reset-password",
                new KeycloakCredential("password", password, false),
                ct);

            return ToResult(userId, response);
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<Guid>(ex, "resetting a password");
        }
    }

    public async Task<Result<Guid>> DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await admin.SendAsync(
                HttpMethod.Delete, $"/admin/realms/{admin.Realm}/users/{userId}", null, ct);

            return ToResult(userId, response);
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<Guid>(ex, "deleting a user");
        }
    }

    private async Task<Result<Guid>> UpdateAsync(
        Guid userId, UpdateUserPayload payload, string operation, CancellationToken ct)
    {
        try
        {
            var response = await admin.SendAsync(
                HttpMethod.Put, $"/admin/realms/{admin.Realm}/users/{userId}", payload, ct);

            return ToResult(userId, response);
        }
        catch (HttpRequestException ex)
        {
            return Unavailable<Guid>(ex, operation);
        }
    }

    private static Result<Guid> ToResult(Guid userId, HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result.Failure<Guid>("That user no longer exists.");

        response.EnsureSuccessStatusCode();
        return Result.Success(userId);
    }

    private Result<T> Unavailable<T>(HttpRequestException exception, string operation)
    {
        // The exception carries no user data, so this is safe to log in full.
        logger.KeycloakUnreachable(exception, operation);
        return Result.Failure<T>("Identity provider is currently unavailable.");
    }

    private static KeycloakAccount ToAccount(KeycloakUser user) => new(
        Guid.Parse(user.Id),
        user.Email ?? user.Username,
        string.Join(' ', new[] { user.FirstName, user.LastName }.Where(p => !string.IsNullOrWhiteSpace(p))).Trim(),
        user.Enabled);

    private static (string FirstName, string LastName) SplitName(string name)
    {
        var parts = name.Trim().Split(' ', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], parts[0]);
    }
}
