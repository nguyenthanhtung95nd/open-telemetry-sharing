using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Services;

/// <summary>One account as the admin screen shows it.</summary>
public sealed record AdminUserView(Guid Id, string Email, string Name, bool Enabled, IReadOnlyList<string> Roles);

// MVC reads a record's validation attributes from its primary constructor, so they belong on
// the parameters. Put them on the properties instead and binding throws at request time.
public sealed record CreateUserRequest(
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    string Email,

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 100 characters or fewer.")]
    string Name,

    [Required(ErrorMessage = "A temporary password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    string Password,

    IReadOnlyList<string>? Roles);

public sealed record UpdateProfileRequest(
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    string Email,

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 100 characters or fewer.")]
    string Name);

public sealed record ResetPasswordRequest(
    [Required(ErrorMessage = "A new password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    string Password);

public sealed record SetRolesRequest(IReadOnlyList<string> Roles);
