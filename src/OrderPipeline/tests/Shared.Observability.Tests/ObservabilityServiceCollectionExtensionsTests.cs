using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Shared.Observability.Options;

namespace Shared.Observability.Tests;

public class ObservabilityServiceCollectionExtensionsTests
{
    private const string ServiceNameAttribute = "service.name";
    private const string ServiceNamespaceAttribute = "service.namespace";
    private const string ServiceVersionAttribute = "service.version";
    private const string DeploymentEnvironmentAttribute = "deployment.environment";

    [Theory]
    [InlineData("OrderPipeline.Api")]
    [InlineData("Inventory.Api")]
    [InlineData("Some.Entirely.Unrelated.Consumer")]
    public void AddSharedObservability_TakesTheServiceNameFromTheConsumer(string serviceName)
    {
        // Act
        var attributes = BuildResourceAttributes(options => options.ServiceName = serviceName);

        // Assert
        Assert.Equal(serviceName, attributes[ServiceNameAttribute]);
    }

    [Fact]
    public void AddSharedObservability_StampsNoNamespaceUnlessTheConsumerAsksForOne()
    {
        // The replaced library hardcoded "OpenTelemetrySharing.Course" onto every consumer.

        // Act
        var attributes = BuildResourceAttributes(options => options.ServiceName = "Any.Consumer");

        // Assert
        Assert.False(attributes.ContainsKey(ServiceNamespaceAttribute));
    }

    [Fact]
    public void AddSharedObservability_StampsNoTeamUnlessTheConsumerAsksForOne()
    {
        // The replaced library hardcoded team=PersonalPractice onto every consumer.

        // Act
        var attributes = BuildResourceAttributes(options => options.ServiceName = "Any.Consumer");

        // Assert
        Assert.False(attributes.ContainsKey("team"));
    }

    [Fact]
    public void AddSharedObservability_StampsTheResourceAttributesTheConsumerSupplies()
    {
        // Act
        var attributes = BuildResourceAttributes(options =>
        {
            options.ServiceName = "Any.Consumer";
            options.ServiceNamespace = "SomeOtherProduct";
            options.ResourceAttributes["team"] = "platform";
            options.ResourceAttributes["cost.centre"] = "CC-42";
        });

        // Assert
        Assert.Equal("SomeOtherProduct", attributes[ServiceNamespaceAttribute]);
        Assert.Equal("platform", attributes["team"]);
        Assert.Equal("CC-42", attributes["cost.centre"]);
    }

    [Fact]
    public void AddSharedObservability_StampsDeploymentEnvironmentWhenSupplied()
    {
        // Act
        var attributes = BuildResourceAttributes(options =>
        {
            options.ServiceName = "Any.Consumer";
            options.DeploymentEnvironment = "production";
        });

        // Assert
        Assert.Equal("production", attributes[DeploymentEnvironmentAttribute]);
    }

    [Fact]
    public void AddSharedObservability_PrefersAnExplicitServiceVersion()
    {
        // Act
        var attributes = BuildResourceAttributes(options =>
        {
            options.ServiceName = "Any.Consumer";
            options.ServiceVersion = "7.3.1";
        });

        // Assert
        Assert.Equal("7.3.1", attributes[ServiceVersionAttribute]);
    }

    [Fact]
    public void AddSharedObservability_BindsFromTheObservabilityConfigurationSection()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:ServiceName"] = "Configured.Api",
            ["Observability:ServiceNamespace"] = "ConfiguredProduct",
            ["Observability:DeploymentEnvironment"] = "staging",
            ["Observability:ResourceAttributes:team"] = "payments"
        });

        // Act
        var attributes = BuildResourceAttributes(configuration, configure: null);

        // Assert
        Assert.Equal("Configured.Api", attributes[ServiceNameAttribute]);
        Assert.Equal("ConfiguredProduct", attributes[ServiceNamespaceAttribute]);
        Assert.Equal("staging", attributes[DeploymentEnvironmentAttribute]);
        Assert.Equal("payments", attributes["team"]);
    }

    [Fact]
    public void AddSharedObservability_LetsCodeOverrideConfiguration()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:ServiceName"] = "FromConfiguration"
        });

        // Act
        var attributes = BuildResourceAttributes(
            configuration, options => options.ServiceName = "FromCode");

        // Assert
        Assert.Equal("FromCode", attributes[ServiceNameAttribute]);
    }

    [Fact]
    public void AddSharedObservability_WhenServiceNameIsMissing_ThrowsNamingTheKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        var exception = Assert.Throws<OptionsValidationException>(
            () => services.AddSharedObservability(configuration));

        // Assert
        Assert.Contains(exception.Failures, failure => failure.Contains("Observability:ServiceName"));
    }

    [Fact]
    public void AddSharedObservability_WhenOtlpEndpointIsNotAUri_ThrowsNamingTheKey()
    {
        // The replaced library let this surface as a bare UriFormatException from host build.

        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:ServiceName"] = "Any.Consumer",
            ["Observability:Export:OtlpEndpoint"] = "not a uri"
        });

        // Act
        var exception = Assert.Throws<OptionsValidationException>(
            () => services.AddSharedObservability(configuration));

        // Assert
        Assert.Contains(exception.Failures, failure => failure.Contains("Observability:Export:OtlpEndpoint"));
    }

    [Fact]
    public void AddSharedObservability_WithNoEndpointAnywhere_LeavesOtlpOff()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedObservability(configuration, options => options.ServiceName = "Any.Consumer");
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<ObservabilityOptions>>();

        // Assert
        Assert.False(options.Value.Export.Otlp);
    }

    [Fact]
    public void AddSharedObservability_TurnsOtlpOnWhenTheStandardEnvironmentKeyIsPresent()
    {
        // docker-compose supplies OTEL_EXPORTER_OTLP_ENDPOINT, which reaches the app as a
        // plain configuration entry, so auto-detection has to read it from there.

        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://otel-collector:4317"
        });

        // Act
        services.AddSharedObservability(configuration, options => options.ServiceName = "Any.Consumer");
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<ObservabilityOptions>>();

        // Assert
        Assert.True(options.Value.Export.Otlp);
    }

    [Fact]
    public void AddSharedObservability_WorksOnABareServiceCollectionWithNoHost()
    {
        // This is the AWS Lambda and console-app path. The replaced library could not be
        // called at all outside ASP.NET Core because it hung off WebApplicationBuilder.

        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedObservability(configuration, options => options.ServiceName = "Sync.Lambda");
        var tracerProvider = services.BuildServiceProvider().GetRequiredService<TracerProvider>();

        // Assert
        Assert.Equal("Sync.Lambda", ReadAttributes(tracerProvider)[ServiceNameAttribute]);
    }

    [Fact]
    public void AddSharedObservability_WorksOnAHostApplicationBuilder()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddSharedObservability(options => options.ServiceName = "Web.Api");
        var tracerProvider = builder.Build().Services.GetRequiredService<TracerProvider>();

        // Assert
        Assert.Equal("Web.Api", ReadAttributes(tracerProvider)[ServiceNameAttribute]);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, object> BuildResourceAttributes(
        Action<ObservabilityOptions> configure) =>
        BuildResourceAttributes(BuildConfiguration(new Dictionary<string, string?>()), configure);

    private static Dictionary<string, object> BuildResourceAttributes(
        IConfiguration configuration, Action<ObservabilityOptions>? configure)
    {
        var services = new ServiceCollection();
        services.AddSharedObservability(configuration, configure);

        return ReadAttributes(services.BuildServiceProvider().GetRequiredService<TracerProvider>());
    }

    private static Dictionary<string, object> ReadAttributes(TracerProvider tracerProvider) =>
        tracerProvider.GetResource().Attributes
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
}
