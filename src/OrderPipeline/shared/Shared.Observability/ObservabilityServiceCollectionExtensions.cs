using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using Shared.Observability.Internal;
using Shared.Observability.Options;

namespace Shared.Observability;

/// <summary>
/// Entry point of the shared observability library. Every value it uses comes from the
/// consuming application, either from the <c>Observability</c> configuration section or from
/// the delegate passed in, so the library itself holds no knowledge of any one product.
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Registers traces, metrics and logs for any host that exposes an
    /// <see cref="IHostApplicationBuilder"/>, which covers both <c>WebApplicationBuilder</c>
    /// and <c>HostApplicationBuilder</c>.
    /// </summary>
    /// <param name="builder">The host builder being configured.</param>
    /// <param name="configure">
    /// Applied after the <c>Observability</c> section is bound, so code wins over configuration.
    /// </param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    /// <remarks>
    /// <c>deployment.environment</c> falls back to the host environment name, so a service
    /// never has to repeat in configuration what the host already knows.
    /// </remarks>
    public static IHostApplicationBuilder AddSharedObservability(
        this IHostApplicationBuilder builder,
        Action<ObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSharedObservability(builder.Configuration, options =>
        {
            configure?.Invoke(options);
            options.DeploymentEnvironment ??= builder.Environment.EnvironmentName;
        });

        return builder;
    }

    /// <summary>
    /// Registers traces, metrics and logs against a bare service collection, for hosts that
    /// have no <see cref="IHostApplicationBuilder"/> such as AWS Lambda or a console app.
    /// </summary>
    /// <param name="services">The container to register into.</param>
    /// <param name="configuration">Configuration root holding the <c>Observability</c> section.</param>
    /// <param name="configure">
    /// Applied after the <c>Observability</c> section is bound, so code wins over configuration.
    /// </param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <exception cref="OptionsValidationException">
    /// Thrown immediately when the resulting options are invalid, naming the offending key.
    /// </exception>
    public static IServiceCollection AddSharedObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ObservabilityOptionsFactory.Create(configuration, configure);

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        if (options.Audit.EventIds.Count > 0)
        {
            services.AddSingleton<ILoggerProvider>(new AuditLoggerProvider(options.Audit));
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => ObservabilityResourceFactory.Configure(resource, options))
            .WithTracing(tracing => TracingConfigurator.Configure(tracing, options))
            .WithMetrics(metrics => MetricsConfigurator.Configure(metrics, options))
            .WithLogging(
                logging => LoggingConfigurator.Configure(logging, options),
                LoggingConfigurator.ConfigureLoggerOptions);

        return services;
    }
}
