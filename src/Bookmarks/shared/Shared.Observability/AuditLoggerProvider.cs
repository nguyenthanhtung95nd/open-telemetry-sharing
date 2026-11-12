using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Observability.Options;

namespace Shared.Observability;

/// <summary>
/// Writes a plain audit line to stdout for the event ids a consumer declares, independently
/// of the configured log levels.
/// </summary>
/// <remarks>
/// A log level change must never be able to silence an audit event, so this provider bypasses
/// level filtering on purpose. It is also the reason the sink is stdout rather than the
/// telemetry pipeline, which can be unavailable exactly when the record matters most.
/// </remarks>
public sealed class AuditLoggerProvider : ILoggerProvider
{
    private readonly AuditLogger _logger;

    /// <summary>Creates a provider auditing the event ids in <paramref name="options"/>.</summary>
    /// <param name="options">The audited event ids.</param>
    public AuditLoggerProvider(ObservabilityAuditOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = new AuditLogger(new HashSet<int>(options.EventIds));
    }

    /// <summary>
    /// Returns the one shared sink for every category. Audited events are selected by event
    /// id, so which category asked for the logger makes no difference.
    /// </summary>
    public ILogger CreateLogger(string categoryName) => _logger;

    /// <summary>Nothing to release. The sink writes to stdout and holds no resource.</summary>
    public void Dispose()
    {
    }
}

/// <summary>Stdout sink for audited events, stamped with the current trace id.</summary>
internal sealed class AuditLogger(HashSet<int> auditedEventIds) : ILogger
{
    private const string NoTrace = "no-trace";

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || !auditedEventIds.Contains(eventId.Id)) return;

        // The trace id is what lets an auditor pivot from this line to the distributed trace.
        var traceId = Activity.Current?.TraceId.ToString() ?? NoTrace;

        Console.WriteLine(
            $"[AUDIT] [{logLevel}({eventId.Id})] [trace:{traceId}]: {formatter(state, exception)}");
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        private NullScope()
        {
        }

        public void Dispose()
        {
        }
    }
}
