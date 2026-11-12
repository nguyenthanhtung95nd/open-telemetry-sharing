using Microsoft.Extensions.Logging;

namespace Inventory.Api.Tests;

/// <summary>
/// Captures what was logged so a test can assert on the event id a message carried.
/// Event ids are the contract the audit sink and the log backend filter on, so a test
/// that only asserts behaviour would not notice one going missing.
/// </summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, int EventId, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, eventId.Id, formatter(state, exception)));
}
