using System.Diagnostics;

namespace Bookmarks.Api.Diagnostics;

/// <summary>
/// One service operation, traced and measured together.
/// </summary>
/// <remarks>
/// Starting a span and starting a timer describe the same event, so one object does both.
/// That way no service method can add a span and forget the metric, which is how two signals
/// quietly drift out of agreement.
/// </remarks>
public readonly struct ServiceOperation : IDisposable, IEquatable<ServiceOperation>
{
    private readonly string _operationName;
    private readonly long _startedAtTicks;

    /// <summary>The span for this operation, or null when nothing is listening.</summary>
    public Activity? Activity { get; }

    private ServiceOperation(string operationName, Activity? activity)
    {
        _operationName = operationName;
        _startedAtTicks = Stopwatch.GetTimestamp();
        Activity = activity;
    }

    /// <summary>Starts a span and begins timing.</summary>
    /// <param name="operationName">Business operation name, from <see cref="ActivityNames"/>.</param>
    /// <returns>A handle that ends the span and records the duration when disposed.</returns>
    public static ServiceOperation Start(string operationName) =>
        new(operationName, ApplicationDiagnostics.ActivitySource.StartActivity(operationName));

    /// <summary>Ends the span and records the elapsed time.</summary>
    public void Dispose()
    {
        ApplicationDiagnostics.OperationDurationHistogram.Record(
            Stopwatch.GetElapsedTime(_startedAtTicks).TotalMilliseconds,
            new KeyValuePair<string, object?>(BookmarkTags.Operation, _operationName));

        Activity?.Dispose();
    }

    /// <summary>Compares this handle with another.</summary>
    /// <param name="other">The handle to compare with.</param>
    /// <returns><c>true</c> when both describe the same started operation.</returns>
    public bool Equals(ServiceOperation other) =>
        _startedAtTicks == other._startedAtTicks && _operationName == other._operationName;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ServiceOperation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_operationName, _startedAtTicks);

    /// <summary>Compares two handles.</summary>
    /// <param name="left">First handle.</param>
    /// <param name="right">Second handle.</param>
    /// <returns><c>true</c> when they describe the same started operation.</returns>
    public static bool operator ==(ServiceOperation left, ServiceOperation right) => left.Equals(right);

    /// <summary>Compares two handles.</summary>
    /// <param name="left">First handle.</param>
    /// <param name="right">Second handle.</param>
    /// <returns><c>true</c> when they describe different started operations.</returns>
    public static bool operator !=(ServiceOperation left, ServiceOperation right) => !left.Equals(right);
}
