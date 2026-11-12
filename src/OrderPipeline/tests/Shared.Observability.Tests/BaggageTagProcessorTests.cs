using System.Diagnostics;
using OpenTelemetry;
using Shared.Observability.Options;

namespace Shared.Observability.Tests;

public sealed class BaggageTagProcessorTests : IDisposable
{
    private const string SourceName = "Shared.Observability.Tests.Baggage";

    private readonly ActivitySource _source = new(SourceName);
    private readonly ActivityListener _listener;

    public BaggageTagProcessorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
        Baggage.Current = default;
    }

    [Fact]
    public void OnEnd_WhenPrefixIsAllowed_CopiesTheEntryOntoTheSpan()
    {
        // Arrange
        var sut = new BaggageTagProcessor(OptionsAllowing("order."));
        Baggage.SetBaggage("order.id", "4821");
        using var activity = _source.StartActivity("checkout")!;

        // Act
        sut.OnEnd(activity);

        // Assert
        Assert.Equal("4821", activity.GetTagItem("order.id"));
    }

    [Theory]
    [InlineData("Order.Id")]
    [InlineData("ORDER.status")]
    public void OnEnd_MatchesThePrefixCaseInsensitively(string baggageKey)
    {
        // Arrange
        var sut = new BaggageTagProcessor(OptionsAllowing("order."));
        Baggage.SetBaggage(baggageKey, "value");
        using var activity = _source.StartActivity("checkout")!;

        // Act
        sut.OnEnd(activity);

        // Assert
        Assert.Equal("value", activity.GetTagItem(baggageKey));
    }

    [Theory]
    [InlineData("customer.name")]
    [InlineData("tenant.id")]
    public void OnEnd_WhenPrefixIsNotAllowed_IgnoresTheEntry(string baggageKey)
    {
        // Arrange
        var sut = new BaggageTagProcessor(OptionsAllowing("order."));
        Baggage.SetBaggage(baggageKey, "value");
        using var activity = _source.StartActivity("checkout")!;

        // Act
        sut.OnEnd(activity);

        // Assert
        Assert.Null(activity.GetTagItem(baggageKey));
    }

    [Fact]
    public void OnEnd_WhenNoPrefixIsAllowed_CopiesNothing()
    {
        // Arrange
        var sut = new BaggageTagProcessor(new ObservabilityBaggageOptions());
        Baggage.SetBaggage("order.id", "4821");
        using var activity = _source.StartActivity("checkout")!;

        // Act
        sut.OnEnd(activity);

        // Assert
        Assert.Empty(activity.TagObjects);
    }

    [Fact]
    public void OnEnd_WhenCallerFloodsBaggage_StopsAtMaxEntries()
    {
        // Baggage is attacker-controllable, so the cap is what stops one caller from
        // stamping unbounded high-cardinality tags onto every span in the system.

        // Arrange
        var options = OptionsAllowing("order.");
        options.MaxEntries = 5;
        var sut = new BaggageTagProcessor(options);
        for (var index = 0; index < 100; index++)
        {
            Baggage.SetBaggage($"order.injected{index}", index.ToString());
        }

        using var activity = _source.StartActivity("checkout")!;

        // Act
        sut.OnEnd(activity);

        // Assert
        Assert.Equal(5, activity.TagObjects.Count());
    }

    [Fact]
    public void OnEnd_WhenValueExceedsTheLimit_TruncatesIt()
    {
        // Arrange
        var options = OptionsAllowing("order.");
        options.MaxValueLength = 32;
        var sut = new BaggageTagProcessor(options);
        Baggage.SetBaggage("order.note", new string('x', 8192));
        using var activity = _source.StartActivity("checkout")!;

        // Act
        sut.OnEnd(activity);

        // Assert
        Assert.Equal(new string('x', 32), activity.GetTagItem("order.note"));
    }

    [Fact]
    public void Constructor_WhenOptionsAreNull_Throws()
    {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new BaggageTagProcessor(null!));
    }

    private static ObservabilityBaggageOptions OptionsAllowing(params string[] prefixes) =>
        new() { AllowedKeyPrefixes = prefixes.ToList() };

    public void Dispose()
    {
        Baggage.Current = default;
        _listener.Dispose();
        _source.Dispose();
    }
}
