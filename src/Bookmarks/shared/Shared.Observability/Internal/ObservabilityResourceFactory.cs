using System.Reflection;
using OpenTelemetry.Resources;
using Shared.Observability.Options;

namespace Shared.Observability.Internal;

/// <summary>Builds the resource describing the process, from consumer-supplied values only.</summary>
internal static class ObservabilityResourceFactory
{
    private const string DeploymentEnvironmentAttribute = "deployment.environment";

    public static void Configure(ResourceBuilder resource, ObservabilityOptions options)
    {
        resource.AddService(
            serviceName: options.ServiceName,
            serviceNamespace: options.ServiceNamespace,
            serviceVersion: options.ServiceVersion ?? ResolveEntryAssemblyVersion(),
            serviceInstanceId: options.ServiceInstanceId);

        var attributes = BuildAttributes(options);
        if (attributes.Count > 0)
        {
            resource.AddAttributes(attributes);
        }
    }

    // The host owns the entry assembly, so a library cannot assume one exists or that it
    // carries a version. Returning null simply leaves service.version off the resource.
    private static string? ResolveEntryAssemblyVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

    private static List<KeyValuePair<string, object>> BuildAttributes(ObservabilityOptions options)
    {
        var attributes = options.ResourceAttributes
            .Select(attribute => new KeyValuePair<string, object>(attribute.Key, attribute.Value))
            .ToList();

        if (!string.IsNullOrWhiteSpace(options.DeploymentEnvironment))
        {
            attributes.Add(new KeyValuePair<string, object>(
                DeploymentEnvironmentAttribute, options.DeploymentEnvironment));
        }

        return attributes;
    }
}
