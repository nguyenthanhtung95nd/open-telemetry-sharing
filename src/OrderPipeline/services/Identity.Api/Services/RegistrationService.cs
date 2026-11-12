using Identity.Api.Diagnostics;
using Identity.Api.Logging;
using Identity.Api.Common;
using Identity.Api.Keycloak;
using Shared.Security;

namespace Identity.Api.Services;

public sealed class RegistrationService(
    IKeycloakUserClient users,
    IKeycloakRoleClient roles,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    public async Task<Result<Guid>> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return Result.Failure<Guid>("Invalid email format.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return Result.Failure<Guid>("Password is required.");

        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("register user");

        var created = await users.CreateAsync(
            new CreateKeycloakUserRequest(request.Email, request.Password, request.Name), ct);
        if (!created.IsSuccess) return created;

        var granted = await roles.GrantAsync(created.Value, [RealmRoles.Customer], ct);
        if (!granted.IsSuccess) return granted;

        // The Keycloak id is enough to find the account. The email is personal data and
        // stays out of the log.
        activity?.SetTag(IdentityTags.UserId, created.Value);
        logger.UserRegistered(created.Value.ToString());

        return created;
    }
}
