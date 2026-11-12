using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Payment.Api.Data;
using Payment.Api.Domain;
using Payment.Api.Logging;
using Payment.Api.Messaging;
using Payment.Api.Simulation;
using Shared.Messaging;
using Xunit;

namespace Payment.Api.Tests;

/// <summary>
/// Phase 4 of the plan switches the consumer to at-least-once delivery, after which the
/// same event can arrive more than once. Charging a customer twice is the worst failure
/// this system could produce, so these assert the guard before that switch is made.
/// </summary>
public class StockReservedHandlerIdempotencyTests
{
    private const int OrderId = 42;

    private readonly IPaymentSimulator _paymentSimulator = Substitute.For<IPaymentSimulator>();
    private readonly IPaymentEventsPublisher _eventsPublisher = Substitute.For<IPaymentEventsPublisher>();
    private readonly RecordingLogger<StockReservedHandler> _logger = new();

    private static readonly StockReservedEvent Event =
        new(OrderId, 99.99m, [new OrderLineItem(1, 2)]);

    [Fact]
    public async Task HandleAsync_CalledTwice_WritesOnePaymentTransaction()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_WritesOnePaymentTransaction));
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        Assert.Single(ReadPayments(provider));
    }

    [Fact]
    public async Task HandleAsync_CalledTwice_DoesNotAskTheSimulatorAgain()
    {
        // Asking again could produce a different answer, which would mean the stored
        // charge and the published event disagreed about what happened.

        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_DoesNotAskTheSimulatorAgain));
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        _paymentSimulator.Received(1).ShouldFail();
    }

    [Fact]
    public async Task HandleAsync_CalledTwiceAfterSuccess_RepublishesPaymentSucceeded()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var provider = BuildProvider(nameof(HandleAsync_CalledTwiceAfterSuccess_RepublishesPaymentSucceeded));
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        _eventsPublisher.Received(2).PublishPaymentSucceeded(OrderId);
        _eventsPublisher.DidNotReceive().PublishPaymentFailed(
            Arg.Any<int>(), Arg.Any<IReadOnlyList<OrderLineItem>>());
    }

    [Fact]
    public async Task HandleAsync_CalledTwiceAfterDecline_RepublishesPaymentFailed()
    {
        // The second delivery must report what is stored, not what the simulator would
        // say this time. The stored row is the only record of what the customer was told.

        // Arrange
        _paymentSimulator.ShouldFail().Returns(true);
        var provider = BuildProvider(nameof(HandleAsync_CalledTwiceAfterDecline_RepublishesPaymentFailed));
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        _paymentSimulator.ShouldFail().Returns(false);
        await sut.HandleAsync(Event);

        // Assert
        _eventsPublisher.Received(2).PublishPaymentFailed(
            OrderId, Arg.Any<IReadOnlyList<OrderLineItem>>());
        _eventsPublisher.DidNotReceive().PublishPaymentSucceeded(Arg.Any<int>());
    }

    [Fact]
    public async Task HandleAsync_CalledTwice_KeepsTheStoredStatus()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(true);
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_KeepsTheStoredStatus));
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        _paymentSimulator.ShouldFail().Returns(false);
        await sut.HandleAsync(Event);

        // Assert
        Assert.Equal(PaymentStatus.Failed, Assert.Single(ReadPayments(provider)).Status);
    }

    [Fact]
    public async Task HandleAsync_OnTheDuplicate_LogsAtInformationWithItsOwnEventId()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var provider = BuildProvider(nameof(HandleAsync_OnTheDuplicate_LogsAtInformationWithItsOwnEventId));
        var sut = BuildHandler(provider);

        // Act
        await sut.HandleAsync(Event);
        await sut.HandleAsync(Event);

        // Assert
        var duplicate = Assert.Single(
            _logger.Entries, entry => entry.EventId == LogEventIds.PaymentAlreadyProcessed);
        Assert.Equal(LogLevel.Information, duplicate.Level);
    }

    [Fact]
    public async Task HandleAsync_CalledTwice_DoesNotWaitTheProcessingDelayAgain()
    {
        // The delay models a payment gateway call. A redelivery has nothing to call.

        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var provider = BuildProvider(nameof(HandleAsync_CalledTwice_DoesNotWaitTheProcessingDelayAgain));
        var sut = BuildHandler(provider, processingDelayMs: 1500);
        await sut.HandleAsync(Event);

        // Act
        var startedAt = DateTime.UtcNow;
        await sut.HandleAsync(Event);
        var elapsed = DateTime.UtcNow - startedAt;

        // Assert
        Assert.True(elapsed < TimeSpan.FromMilliseconds(750), $"duplicate took {elapsed.TotalMilliseconds}ms");
    }

    private StockReservedHandler BuildHandler(ServiceProvider provider, int processingDelayMs = 0) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(),
            _paymentSimulator,
            _eventsPublisher,
            Options.Create(new PaymentOptions { ProcessingDelayMs = processingDelayMs }),
            _logger);

    private static ServiceProvider BuildProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static List<PaymentTransaction> ReadPayments(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Payments.AsNoTracking().ToList();
    }
}
