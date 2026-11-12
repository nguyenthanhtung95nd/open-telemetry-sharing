using System.Diagnostics;

namespace Bookmarks.Api.Middleware;

/// <summary>
/// Returns the current trace id to the caller so a browser can show it to a user, who can
/// then quote it in a support request and have it found in the telemetry backend.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
public sealed class TraceIdResponseMiddleware(RequestDelegate next)
{
    /// <summary>Response header carrying the trace id.</summary>
    public const string HeaderName = "X-Trace-Id";

    /// <summary>Attaches the trace id to the response.</summary>
    /// <param name="context">The request being served.</param>
    /// <returns>A task that completes when the rest of the pipeline has run.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // OnStarting, because headers can no longer be set once the response body begins.
        context.Response.OnStarting(() =>
        {
            if (Activity.Current?.TraceId.ToString() is { Length: > 0 } traceId)
            {
                context.Response.Headers[HeaderName] = traceId;
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
