namespace Gateway.Api.Logging;

/// <summary>
/// Event ids for this service. The 1000 range belongs to Gateway.Api.
/// </summary>
public static class LogEventIds
{
    /// <summary>A request was proxied to a downstream service and answered.</summary>
    public const int RequestProxied = 1001;

    /// <summary>A proxied request came back with a server error.</summary>
    public const int DownstreamFailed = 1002;

    /// <summary>A request was rejected before reaching a downstream service.</summary>
    public const int RequestRejected = 1003;
}

/// <summary>
/// Every log message Gateway.Api emits.
/// </summary>
/// <remarks>
/// The gateway is the only place that sees every request entering the estate, so its log is
/// the first thing read during an incident. Query strings are deliberately not logged, as
/// they routinely carry tokens and personal data.
/// </remarks>
public static partial class GatewayLog
{
    /// <summary>Logs a request that was proxied and answered normally.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="method">HTTP method of the request.</param>
    /// <param name="path">Request path, without the query string.</param>
    /// <param name="statusCode">Status code returned to the caller.</param>
    /// <param name="elapsedMs">Round trip time in milliseconds.</param>
    [LoggerMessage(
        EventId = LogEventIds.RequestProxied,
        Level = LogLevel.Information,
        Message = "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms")]
    public static partial void RequestProxied(
        this ILogger logger, string method, string path, int statusCode, double elapsedMs);

    /// <summary>Logs a proxied request that came back with a server error.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="method">HTTP method of the request.</param>
    /// <param name="path">Request path, without the query string.</param>
    /// <param name="statusCode">Status code returned to the caller.</param>
    /// <param name="elapsedMs">Round trip time in milliseconds.</param>
    [LoggerMessage(
        EventId = LogEventIds.DownstreamFailed,
        Level = LogLevel.Error,
        Message = "{Method} {Path} failed downstream with {StatusCode} after {ElapsedMs}ms")]
    public static partial void DownstreamFailed(
        this ILogger logger, string method, string path, int statusCode, double elapsedMs);

    /// <summary>Logs a request the gateway refused before proxying it.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="method">HTTP method of the request.</param>
    /// <param name="path">Request path, without the query string.</param>
    /// <param name="statusCode">Status code returned to the caller.</param>
    [LoggerMessage(
        EventId = LogEventIds.RequestRejected,
        Level = LogLevel.Warning,
        Message = "{Method} {Path} rejected at the gateway with {StatusCode}")]
    public static partial void RequestRejected(
        this ILogger logger, string method, string path, int statusCode);
}
