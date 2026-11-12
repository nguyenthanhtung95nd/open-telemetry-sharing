using System.Diagnostics.Metrics;
using OrderPipeline.Api.Diagnostics;
using OrderPipeline.Api.Domain;
using Shared.Messaging;
using Xunit;

namespace OrderPipeline.Api.Tests;

/// <summary>
/// These assert the instruments a stalled-saga alert is built on. A handler that stops
/// updating one of them makes the alert quietly useless, which is worse than no alert.
/// </summary>
public sealed class OrderMetricsTests : IDisposable
{
    private const string MeterName = "OrderPipeline.Api";

    private readonly List<(string Instrument, long Value, string? Status)> _counts = [];
    private readonly List<(string Instrument, double Value, string? Status)> _measurements = [];
    private readonly MeterListener _listener;

    public OrderMetricsTests()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == MeterName) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>(RecordLong);
        _listener.SetMeasurementEventCallback<double>(RecordDouble);
        _listener.Start();
    }

    [Fact]
    public void RecordAccepted_CountsTheOrderAndPutsItInFlight()
    {
        // Act
        OrderMetrics.RecordAccepted(CreateOrder(OrderStatus.Pending, DateTime.UtcNow));

        // Assert
        Assert.Contains(_counts, m => m.Instrument == "orders.created" && m.Value == 1);
        Assert.Contains(_counts, m => m.Instrument == "orders.in_flight" && m.Value == 1);
    }

    [Fact]
    public void RecordAccepted_DoesNotTagTheCreatedCounterWithAStatusThatIsAlwaysPending()
    {
        // The counter used to carry order.status, which is Pending for every order ever
        // created. A tag with one possible value costs cardinality and says nothing.

        // Act
        OrderMetrics.RecordAccepted(CreateOrder(OrderStatus.Pending, DateTime.UtcNow));

        // Assert
        Assert.All(
            _counts.Where(m => m.Instrument == "orders.created"),
            m => Assert.Null(m.Status));
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.PaymentFailed)]
    [InlineData(OrderStatus.OutOfStock)]
    public void RecordSettled_CountsTheTerminalStatusAndTakesTheOrderOutOfFlight(OrderStatus status)
    {
        // Act
        OrderMetrics.RecordSettled(CreateOrder(status, DateTime.UtcNow));

        // Assert
        Assert.Contains(_counts, m =>
            m.Instrument == "orders.terminal" && m.Value == 1 && m.Status == status.ToString());
        Assert.Contains(_counts, m => m.Instrument == "orders.in_flight" && m.Value == -1);
    }

    [Fact]
    public void RecordSettled_MeasuresTheTimeSinceTheOrderWasCreated()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-2);

        // Act
        OrderMetrics.RecordSettled(CreateOrder(OrderStatus.Paid, createdAt));

        // Assert
        var duration = Assert.Single(
            _measurements, m => m.Instrument == "orders.fulfilment.duration");
        Assert.InRange(duration.Value, TimeSpan.FromMinutes(2).TotalMilliseconds, TimeSpan.FromMinutes(3).TotalMilliseconds);
        Assert.Equal(nameof(OrderStatus.Paid), duration.Status);
    }

    [Fact]
    public void RecordAccepted_WhenTheOrderIsNull_Throws()
    {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => OrderMetrics.RecordAccepted(null!));
    }

    [Fact]
    public void RecordSettled_WhenTheOrderIsNull_Throws()
    {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => OrderMetrics.RecordSettled(null!));
    }

    private void RecordLong(
        Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _) =>
        _counts.Add((instrument.Name, value, ReadStatus(tags)));

    private void RecordDouble(
        Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _) =>
        _measurements.Add((instrument.Name, value, ReadStatus(tags)));

    private static string? ReadStatus(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == OrderPipelineTags.OrderStatus) return tag.Value as string;
        }

        return null;
    }

    private static Order CreateOrder(OrderStatus status, DateTime createdAtUtc) => new()
    {
        Id = 4821,
        CustomerName = "Test Customer",
        CustomerEmail = "test@example.com",
        Status = status,
        CreatedAtUtc = createdAtUtc,
        TotalAmount = 250m,
        Items = [new OrderItem { ProductId = 7, Quantity = 2, UnitPrice = 125m }]
    };

    public void Dispose() => _listener.Dispose();
}
