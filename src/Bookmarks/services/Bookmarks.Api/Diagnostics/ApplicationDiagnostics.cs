using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bookmarks.Api.Diagnostics;

/// <summary>Instruments owned by this service, registered with the SDK in Program.cs.</summary>
/// <remarks>
/// This is the only place an activity source or an instrument is constructed. Nothing else
/// in the service calls <c>new ActivitySource</c> or <c>CreateCounter</c>, so the set of
/// names the SDK must be told about is exactly the set declared here.
/// </remarks>
public static class ApplicationDiagnostics
{
    /// <summary>Name shared by this service's activity source and meter.</summary>
    public const string ServiceName = "Bookmarks.Api";

    /// <summary>
    /// Activity source the Npgsql driver publishes database spans on.
    /// </summary>
    /// <remarks>
    /// Not owned by this service, but named here because subscribing to it is what turns
    /// database spans on. The shared observability library exposes no option for database
    /// instrumentation, and none is needed: naming the source is enough.
    /// </remarks>
    public const string NpgsqlActivitySourceName = "Npgsql";

    /// <summary>Activity source for spans this service starts by hand.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>Meter carrying this service's business metrics.</summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Bookmarks accepted and persisted.</summary>
    public static readonly Counter<long> BookmarksCreatedCounter =
        Meter.CreateCounter<long>("bookmarks.created");

    /// <summary>
    /// Time spent inside a service operation, tagged by operation name.
    /// </summary>
    /// <remarks>
    /// Deliberately not the same as the HTTP server duration ASP.NET Core already reports.
    /// This excludes model binding, serialization and middleware, so comparing the two shows
    /// how much of a slow request is actually the business operation.
    /// </remarks>
    public static readonly Histogram<double> OperationDurationHistogram =
        Meter.CreateHistogram<double>("bookmarks.operation.duration", unit: "ms");
}
