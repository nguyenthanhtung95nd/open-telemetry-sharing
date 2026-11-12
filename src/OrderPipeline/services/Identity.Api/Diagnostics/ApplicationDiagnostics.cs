using System.Diagnostics;

namespace Identity.Api.Diagnostics;

/// <summary>Instruments owned by this service, registered with the SDK in Program.cs.</summary>
public static class ApplicationDiagnostics
{
    private const string ServiceName = "Identity.Api";

    /// <summary>
    /// Activity source for spans this service starts by hand. An administrative change is
    /// several Keycloak calls behind one business intent, and only a span of its own groups
    /// them into something a reader can follow.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}
