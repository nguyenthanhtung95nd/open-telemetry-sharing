using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Payment.Api.Diagnostics;

/// <summary>Instruments owned by this service, registered with the SDK in Program.cs.</summary>
public static class ApplicationDiagnostics
{
    private const string ServiceName = "Payment.Api";

    /// <summary>Meter carrying this service's business metrics.</summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Activity source for spans this service starts by hand.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>Payments attempted, tagged by outcome.</summary>
    public static readonly Counter<long> PaymentsProcessedCounter =
        Meter.CreateCounter<long>("payments.processed");
}
