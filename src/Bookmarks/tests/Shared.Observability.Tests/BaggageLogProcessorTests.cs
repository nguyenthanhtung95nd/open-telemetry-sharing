using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Shared.Observability.Options;

namespace Shared.Observability.Tests;

public sealed class BaggageLogProcessorTests : IDisposable
{
    private readonly List<KeyValuePair<string, object?>[]> _captured = [];

    public BaggageLogProcessorTests() => Baggage.Current = default;

    [Fact]
    public void OnEnd_WhenPrefixIsAllowed_AddsTheBaggageToTheLogRecord()
    {
        // Arrange
        Baggage.SetBaggage("order.id", "4821");

        // Act
        Log(OptionsAllowing("order."));

        // Assert
        Assert.Contains(_captured[0], attribute => attribute.Key == "order.id" && (string?)attribute.Value == "4821");
    }

    [Fact]
    public void OnEnd_KeepsTheAttributesTheCallSiteAlreadyWrote()
    {
        // The processor must add to the record, never replace what the message template
        // produced, or every structured field in the log backend disappears.

        // Arrange
        Baggage.SetBaggage("order.id", "4821");

        // Act
        Log(OptionsAllowing("order."));

        // Assert
        Assert.Contains(_captured[0], attribute => attribute.Key == "ProductId");
    }

    [Theory]
    [InlineData("customer.name")]
    [InlineData("tenant.id")]
    public void OnEnd_WhenPrefixIsNotAllowed_LeavesTheEntryOut(string baggageKey)
    {
        // Arrange
        Baggage.SetBaggage(baggageKey, "value");

        // Act
        Log(OptionsAllowing("order."));

        // Assert
        Assert.DoesNotContain(_captured[0], attribute => attribute.Key == baggageKey);
    }

    [Fact]
    public void OnEnd_WhenNoPrefixIsAllowed_AddsNothing()
    {
        // Arrange
        Baggage.SetBaggage("order.id", "4821");

        // Act
        Log(new ObservabilityBaggageOptions());

        // Assert
        Assert.DoesNotContain(_captured[0], attribute => attribute.Key == "order.id");
    }

    [Fact]
    public void OnEnd_WhenCallerFloodsBaggage_StopsAtMaxEntries()
    {
        // Arrange
        var options = OptionsAllowing("order.");
        options.MaxEntries = 3;
        for (var index = 0; index < 50; index++) Baggage.SetBaggage($"order.x{index}", index.ToString());

        // Act
        Log(options);

        // Assert
        Assert.Equal(3, _captured[0].Count(attribute => attribute.Key.StartsWith("order.x")));
    }

    [Fact]
    public void OnEnd_WhenValueExceedsTheLimit_TruncatesIt()
    {
        // Arrange
        var options = OptionsAllowing("order.");
        options.MaxValueLength = 8;
        Baggage.SetBaggage("order.note", new string('x', 500));

        // Act
        Log(options);

        // Assert
        var note = _captured[0].Single(attribute => attribute.Key == "order.note");
        Assert.Equal(new string('x', 8), note.Value);
    }

    private void Log(ObservabilityBaggageOptions options)
    {
        using var loggerFactory = LoggerFactory.Create(logging => logging.AddOpenTelemetry(telemetry =>
        {
            telemetry.ParseStateValues = true;
            telemetry.AddProcessor(new BaggageLogProcessor(options));
            telemetry.AddProcessor(new CaptureProcessor(_captured));
        }));

        loggerFactory.CreateLogger("Test").LogInformation("Checking {ProductId}", 7);
    }

    private static ObservabilityBaggageOptions OptionsAllowing(params string[] prefixes) =>
        new() { AllowedKeyPrefixes = prefixes.ToList() };

    public void Dispose() => Baggage.Current = default;

    private sealed class CaptureProcessor(List<KeyValuePair<string, object?>[]> sink)
        : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data) =>
            sink.Add(data.Attributes?.ToArray() ?? []);
    }
}
