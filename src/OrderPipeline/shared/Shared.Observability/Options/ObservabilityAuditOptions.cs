namespace Shared.Observability.Options;

/// <summary>
/// Log events that must reach stdout no matter how log levels are filtered, because losing
/// them to a configuration change would leave a gap in the audit record.
/// </summary>
public sealed class ObservabilityAuditOptions
{
    /// <summary>
    /// Event ids treated as audit events. Empty, the default, registers no audit sink at all.
    /// </summary>
    public IList<int> EventIds { get; set; } = new List<int>();
}
