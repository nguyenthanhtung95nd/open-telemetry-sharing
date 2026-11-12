using System.Diagnostics;

namespace OrderPipeline.Api.Logging;

public sealed class TimedOperation : IDisposable
{
    private readonly ILogger _logger;
    private readonly LogLevel _logLevel;
    private readonly string _messageTemplate;
    private readonly object?[] _args;
    private readonly long _startingTimestamp;

    public TimedOperation(ILogger logger, LogLevel logLevel, string messageTemplate, object?[] args)
    {
        _logger = logger;
        _logLevel = logLevel;
        _messageTemplate = messageTemplate;
        _args = new object[args.Length + 1];
        Array.Copy(args, _args, args.Length);
        _startingTimestamp = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        var delta = Stopwatch.GetElapsedTime(_startingTimestamp);
        _args[^1] = delta.TotalMilliseconds;
        _logger.Log(_logLevel, $"{_messageTemplate} completed in {{ElapsedMs}}ms", _args);
    }
}

public static class TimedOperationExtensions
{
    public static IDisposable BeginTimedOperation(
        this ILogger logger, string messageTemplate, params object[] args)
    {
        return BeginTimedOperation(logger, LogLevel.Information, messageTemplate, args);
    }

    public static IDisposable BeginTimedOperation(
        this ILogger logger, LogLevel logLevel, string messageTemplate, params object[] args)
    {
        return new TimedOperation(logger, logLevel, messageTemplate, args);
    }
}
