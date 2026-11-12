using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookmarks.Api.Middleware;

/// <summary>
/// Last line of defence. Turns an unhandled exception into a problem response rather than an
/// empty 500, and makes sure it is logged before it leaves the process.
/// </summary>
/// <param name="logger">Logger for this handler.</param>
/// <remarks>
/// The response deliberately carries no exception message or stack trace. Those reach the
/// log and the trace, which are access controlled. The response body is not.
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        logger.LogError(exception, "Unhandled exception while serving {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The failure has been logged. Quote the trace id if you report it."
            },
            cancellationToken);

        return true;
    }
}
