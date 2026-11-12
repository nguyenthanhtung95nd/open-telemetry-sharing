using Shared.Observability.Options;

namespace Shared.Observability.Tests;

public class ObservabilityOptionsValidatorTests
{
    private readonly ObservabilityOptionsValidator _sut = new();

    [Fact]
    public void Validate_WhenServiceNameIsMissing_FailsAndNamesTheKey()
    {
        // Arrange
        var options = new ObservabilityOptions();

        // Act
        var result = _sut.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("Observability:ServiceName"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(42.0)]
    public void Validate_WhenRatioIsOutOfRange_FailsAndNamesTheKey(double ratio)
    {
        // Arrange
        var options = ValidOptions();
        options.Sampling.Mode = SamplingMode.ParentBasedTraceIdRatio;
        options.Sampling.Ratio = ratio;

        // Act
        var result = _sut.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("Observability:Sampling:Ratio"));
    }

    [Fact]
    public void Validate_WhenRatioIsOutOfRangeButModeIgnoresIt_Succeeds()
    {
        // Arrange
        var options = ValidOptions();
        options.Sampling.Mode = SamplingMode.AlwaysOn;
        options.Sampling.Ratio = 42.0;

        // Act
        var result = _sut.Validate(name: null, options);

        // Assert
        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_WhenOtlpEndpointIsNotAbsolute_FailsAndNamesTheKey()
    {
        // Arrange
        var options = ValidOptions();
        options.Export.OtlpEndpoint = "not a uri";

        // Act
        var result = _sut.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("Observability:Export:OtlpEndpoint"));
    }

    [Theory]
    [InlineData(-1, 256)]
    [InlineData(16, -1)]
    public void Validate_WhenBaggageLimitsAreNegative_Fails(int maxEntries, int maxValueLength)
    {
        // Arrange
        var options = ValidOptions();
        options.Baggage.MaxEntries = maxEntries;
        options.Baggage.MaxValueLength = maxValueLength;

        // Act
        var result = _sut.Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_WhenEverythingIsSet_Succeeds()
    {
        // Arrange
        var options = ValidOptions();

        // Act
        var result = _sut.Validate(name: null, options);

        // Assert
        Assert.False(result.Failed);
    }

    private static ObservabilityOptions ValidOptions() => new() { ServiceName = "Any.Consumer" };
}
