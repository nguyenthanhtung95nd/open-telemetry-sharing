using System.Diagnostics;
using Gateway.Api.Logging;

namespace Gateway.Api.Middleware;

/// <summary>
/// Writes one structured line per request passing through the gateway.
/// </summary>
/// <remarks>
/// This is the estate's front door, so this line is what tells an operator whether a problem
/// is reaching the backend at all. The level is chosen from the status code, which lets an
/// alert key on the error line rather than on parsing a status out of a message.
/// </remarks>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const int ServerErrorThreshold = 500;
    private const int ClientErrorThreshold = 400;

    /// <summary>Times the rest of the pipeline and logs the outcome.</summary>
    /// <param name="context">The request being handled.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            Log(context, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }

    private void Log(HttpContext context, double elapsedMs)
    {
        var method = context.Request.Method;

        // The path is logged without its query string, which routinely carries tokens.
        var path = context.Request.Path.Value ?? "/";
        var status = context.Response.StatusCode;

        if (status >= ServerErrorThreshold)
        {
            logger.DownstreamFailed(method, path, status, elapsedMs);
            return;
        }

        if (status >= ClientErrorThreshold)
        {
            logger.RequestRejected(method, path, status);
            return;
        }

        logger.RequestProxied(method, path, status, elapsedMs);
    }
}
