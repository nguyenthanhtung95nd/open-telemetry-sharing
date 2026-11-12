using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>
/// Resolves the effective options once, at composition time, and refuses to hand back an
/// invalid set. Failing here means the operator sees the offending configuration key rather
/// than an exception thrown from inside the SDK minutes later.
/// </summary>
internal static class ObservabilityOptionsFactory
{
    public static ObservabilityOptions Create(
        IConfiguration configuration, Action<ObservabilityOptions>? configure)
    {
        var options = new ObservabilityOptions();
        configuration.GetSection(ObservabilityOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        options.Export.Otlp ??= HasOtlpEndpoint(options, configuration);

        EnsureValid(options);
        return options;
    }

    private static bool HasOtlpEndpoint(ObservabilityOptions options, IConfiguration configuration)
    {
        var endpoint = options.Export.OtlpEndpoint
                       ?? configuration[OtlpExporterConfigurator.EndpointConfigurationKey];

        return !string.IsNullOrWhiteSpace(endpoint);
    }

    private static void EnsureValid(ObservabilityOptions options)
    {
        var result = new ObservabilityOptionsValidator().Validate(Microsoft.Extensions.Options.Options.DefaultName, options);
        if (result.Failed)
        {
            throw new OptionsValidationException(
                ObservabilityOptions.SectionName, typeof(ObservabilityOptions), result.Failures);
        }
    }
}
