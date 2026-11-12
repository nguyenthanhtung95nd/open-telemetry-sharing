using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Observability.Options;

namespace Shared.Observability.Tests;

public class AuditLoggerProviderTests
{
    private const int AuditedEventId = 2001;
    private const int OrdinaryEventId = 2099;

    [Fact]
    public void Log_WhenTheEventIsAudited_WritesALineToStdout()
    {
        // Act
        var output = Capture(logger =>
            logger.Log(LogLevel.Information, AuditedEventId, "Order 4821 created"));

        // Assert
        Assert.Contains("[AUDIT]", output);
        Assert.Contains("Order 4821 created", output);
    }

    [Fact]
    public void Log_WhenTheEventIsNotAudited_WritesNothing()
    {
        // Act
        var output = Capture(logger =>
            logger.Log(LogLevel.Information, OrdinaryEventId, "Routine chatter"));

        // Assert
        Assert.Equal(string.Empty, output.Trim());
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Critical)]
    public void Log_WritesAtEveryLevel(LogLevel level)
    {
        // A log level change must never be able to silence an audit event, which is the
        // whole reason this provider bypasses level filtering.

        // Act
        var output = Capture(logger => logger.Log(level, AuditedEventId, "Audited"));

        // Assert
        Assert.Contains("Audited", output);
    }

    [Fact]
    public void Log_StampsTheCurrentTraceId()
    {
        // Arrange
        using var source = new ActivitySource(nameof(Log_StampsTheCurrentTraceId));
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == nameof(Log_StampsTheCurrentTraceId),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = source.StartActivity("checkout")!;

        // Act
        var output = Capture(logger => logger.Log(LogLevel.Information, AuditedEventId, "Audited"));

        // Assert
        Assert.Contains($"[trace:{activity.TraceId}]", output);
    }

    [Fact]
    public void Log_WhenThereIsNoTrace_SaysSo()
    {
        // Act
        var output = Capture(logger => logger.Log(LogLevel.Information, AuditedEventId, "Audited"));

        // Assert
        Assert.Contains("[trace:no-trace]", output);
    }

    [Fact]
    public void Constructor_WhenOptionsAreNull_Throws()
    {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new AuditLoggerProvider(null!));
    }

    private static string Capture(Action<ILogger> write)
    {
        var options = new ObservabilityAuditOptions { EventIds = [AuditedEventId] };
        using var provider = new AuditLoggerProvider(options);
        var logger = provider.CreateLogger("Test");

        var original = Console.Out;
        using var buffer = new StringWriter();
        Console.SetOut(buffer);
        try
        {
            write(logger);
        }
        finally
        {
            Console.SetOut(original);
        }

        return buffer.ToString();
    }
}
