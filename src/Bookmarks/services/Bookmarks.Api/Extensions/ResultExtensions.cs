using Bookmarks.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookmarks.Api.Extensions;

/// <summary>
/// Turns a failed <see cref="Result"/> into an RFC 7807 response.
/// </summary>
/// <remarks>
/// Lives here rather than in each controller so every endpoint maps the same failure kind to
/// the same status code. A controller that chose its own mapping would make the API's error
/// contract depend on which endpoint you happened to call.
/// </remarks>
public static class ResultExtensions
{
    /// <summary>Maps a failed result onto the matching problem response.</summary>
    /// <param name="result">The failed result. Passing a successful result is a bug.</param>
    /// <returns>A 400, 404 or 409 response carrying problem details.</returns>
    /// <exception cref="InvalidOperationException">The result was successful.</exception>
    public static IActionResult ToProblemResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result has no problem to report.");
        }

        var (status, title) = result.ErrorKind switch
        {
            ResultErrorKind.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ResultErrorKind.Conflict => (StatusCodes.Status409Conflict, "Conflict with current state"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request")
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = result.Error
        })
        {
            StatusCode = status
        };
    }
}
