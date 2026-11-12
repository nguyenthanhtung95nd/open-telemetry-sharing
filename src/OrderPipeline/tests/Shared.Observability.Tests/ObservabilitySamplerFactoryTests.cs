using System.Diagnostics;
using OpenTelemetry.Trace;
using Shared.Observability.Internal;
using Shared.Observability.Options;

namespace Shared.Observability.Tests;

public class ObservabilitySamplerFactoryTests
{
    [Fact]
    public void Create_ForDefaultMode_ReturnsNullSoTheSdkDefaultSurvives()
    {
        // Leaving the SDK default in place is what keeps OTEL_TRACES_SAMPLER working and
        // keeps every span reaching a collector that performs tail sampling.

        // Act
        var sampler = ObservabilitySamplerFactory.Create(new ObservabilitySamplingOptions());

        // Assert
        Assert.Null(sampler);
    }

    [Theory]
    [InlineData(SamplingMode.AlwaysOn, typeof(AlwaysOnSampler))]
    [InlineData(SamplingMode.AlwaysOff, typeof(AlwaysOffSampler))]
    [InlineData(SamplingMode.TraceIdRatio, typeof(TraceIdRatioBasedSampler))]
    [InlineData(SamplingMode.ParentBasedTraceIdRatio, typeof(ParentBasedSampler))]
    public void Create_MapsEachModeOntoTheSdkSampler(SamplingMode mode, Type expected)
    {
        // Arrange
        var sampling = new ObservabilitySamplingOptions { Mode = mode, Ratio = 0.5 };

        // Act
        var sampler = ObservabilitySamplerFactory.Create(sampling);

        // Assert
        Assert.IsType(expected, sampler);
    }

    [Fact]
    public void Create_ForTraceIdRatio_ReturnsTheSameDecisionForOneTraceId()
    {
        // The replaced RateSampler drew a fresh random number per span, so spans of one
        // trace disagreed and traces arrived shattered.

        // Arrange
        var sampler = ObservabilitySamplerFactory.Create(
            new ObservabilitySamplingOptions { Mode = SamplingMode.TraceIdRatio, Ratio = 0.5 })!;
        var traceId = ActivityTraceId.CreateRandom();
        var decisions = new HashSet<SamplingDecision>();

        // Act
        for (var trial = 0; trial < 200; trial++)
        {
            var parameters = new SamplingParameters(default, traceId, "span", ActivityKind.Internal);
            decisions.Add(sampler.ShouldSample(parameters).Decision);
        }

        // Assert
        Assert.Single(decisions);
    }

    [Fact]
    public void Create_ForParentBasedTraceIdRatio_HonoursASampledRemoteParent()
    {
        // The replaced RateSampler ignored the parent entirely, so a child of an already
        // sampled remote parent could be dropped and the distributed trace broke apart.

        // Arrange
        var sampler = ObservabilitySamplerFactory.Create(new ObservabilitySamplingOptions
        {
            Mode = SamplingMode.ParentBasedTraceIdRatio,
            Ratio = 0.0
        })!;
        var sampledParent = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded,
            isRemote: true);
        var parameters = new SamplingParameters(
            sampledParent, sampledParent.TraceId, "child", ActivityKind.Internal);

        // Act
        var result = sampler.ShouldSample(parameters);

        // Assert
        Assert.Equal(SamplingDecision.RecordAndSample, result.Decision);
    }
}
