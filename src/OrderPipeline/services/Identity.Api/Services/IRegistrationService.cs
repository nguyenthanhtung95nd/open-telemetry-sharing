using Identity.Api.Common;

namespace Identity.Api.Services;

public sealed record RegisterUserRequest(string Email, string Password, string Name);

public interface IRegistrationService
{
    Task<Result<Guid>> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);
}
