using Microsoft.Extensions.Options;

namespace Shared.Observability.Options;

/// <summary>
/// Turns a misconfigured <see cref="ObservabilityOptions"/> into a startup failure that
/// names the offending configuration key, instead of an exception from inside the SDK.
/// </summary>
public sealed class ObservabilityOptionsValidator : IValidateOptions<ObservabilityOptions>
{
    /// <summary>Reports every invalid key at once, so one restart surfaces all of them.</summary>
    public ValidateOptionsResult Validate(string? name, ObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ValidateService(options, failures);
        ValidateSampling(options.Sampling, failures);
        ValidateBaggage(options.Baggage, failures);
        ValidateExport(options.Export, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateService(ObservabilityOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
            failures.Add(Key("ServiceName") + " must be set to the logical name of this service.");
    }

    private static void ValidateSampling(ObservabilitySamplingOptions sampling, List<string> failures)
    {
        var usesRatio = sampling.Mode is SamplingMode.TraceIdRatio or SamplingMode.ParentBasedTraceIdRatio;
        if (usesRatio && sampling.Ratio is < 0 or > 1)
            failures.Add(Key("Sampling:Ratio") + $" must be between 0 and 1 but was {sampling.Ratio}.");
    }

    private static void ValidateBaggage(ObservabilityBaggageOptions baggage, List<string> failures)
    {
        if (baggage.MaxEntries < 0)
            failures.Add(Key("Baggage:MaxEntries") + $" cannot be negative but was {baggage.MaxEntries}.");

        if (baggage.MaxValueLength < 0)
            failures.Add(Key("Baggage:MaxValueLength") + $" cannot be negative but was {baggage.MaxValueLength}.");
    }

    private static void ValidateExport(ObservabilityExportOptions export, List<string> failures)
    {
        if (export.OtlpEndpoint is { Length: > 0 } endpoint
            && !Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
            failures.Add(Key("Export:OtlpEndpoint") + $" must be an absolute URI but was '{endpoint}'.");
    }

    private static string Key(string suffix) => $"{ObservabilityOptions.SectionName}:{suffix}";
}
