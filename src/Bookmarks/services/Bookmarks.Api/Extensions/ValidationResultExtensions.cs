using FluentValidation.Results;

namespace Bookmarks.Api.Extensions;

/// <summary>Turns a failed validation into a single human-readable sentence.</summary>
/// <remarks>
/// Shared so that every endpoint formats its validation errors the same way. Two services
/// each holding their own copy of this is how one of them quietly ends up using a different
/// separator from the other.
/// </remarks>
public static class ValidationResultExtensions
{
    /// <summary>Joins every failure message into one string.</summary>
    /// <param name="validation">The validation outcome to describe.</param>
    /// <returns>All failure messages, separated by spaces.</returns>
    public static string Describe(this ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        return string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
    }
}
