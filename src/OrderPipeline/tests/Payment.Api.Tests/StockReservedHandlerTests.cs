using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Payment.Api.Data;
using Payment.Api.Domain;
using Payment.Api.Messaging;
using Payment.Api.Simulation;
using Shared.Messaging;
using Xunit;

namespace Payment.Api.Tests;

public class StockReservedHandlerTests
{
    private readonly IPaymentSimulator _paymentSimulator = Substitute.For<IPaymentSimulator>();
    private readonly IPaymentEventsPublisher _eventsPublisher = Substitute.For<IPaymentEventsPublisher>();

    private static IServiceScopeFactory BuildScopeFactory(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private StockReservedHandler BuildHandler(IServiceScopeFactory scopeFactory, int processingDelayMs = 0) =>
        new(scopeFactory,
            _paymentSimulator,
            _eventsPublisher,
            Options.Create(new PaymentOptions { ProcessingDelayMs = processingDelayMs }),
            NullLogger<StockReservedHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WhenSimulatorSucceeds_PersistsSucceededAndPublishesPaymentSucceeded()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenSimulatorSucceeds_PersistsSucceededAndPublishesPaymentSucceeded));
        var sut = BuildHandler(scopeFactory);
        var @event = new StockReservedEvent(42, 99.99m, [new OrderLineItem(1, 2)]);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        _eventsPublisher.Received(1).PublishPaymentSucceeded(42);
        _eventsPublisher.DidNotReceive().PublishPaymentFailed(Arg.Any<int>(), Arg.Any<IReadOnlyList<OrderLineItem>>());

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = await context.Payments.FirstAsync(p => p.OrderId == 42);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task HandleAsync_WhenSimulatorFails_PersistsFailedAndPublishesPaymentFailedWithItems()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(true);
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenSimulatorFails_PersistsFailedAndPublishesPaymentFailedWithItems));
        var sut = BuildHandler(scopeFactory);
        var items = new List<OrderLineItem> { new(1, 2) };
        var @event = new StockReservedEvent(42, 99.99m, items);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        _eventsPublisher.Received(1).PublishPaymentFailed(42, items);
        _eventsPublisher.DidNotReceive().PublishPaymentSucceeded(Arg.Any<int>());

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = await context.Payments.FirstAsync(p => p.OrderId == 42);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenSimulatorFails_MarksTheAuthorizationSpanAsError()
    {
        // The error status is set on the handler's own "authorize payment" span rather than
        // on whatever span happened to be current. In production the ambient span is the
        // RabbitMQ consume span, and a declined payment is not a messaging failure.

        // Arrange
        _paymentSimulator.ShouldFail().Returns(true);
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenSimulatorFails_MarksTheAuthorizationSpanAsError));
        var sut = BuildHandler(scopeFactory);
        var finished = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = finished.Add
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        await sut.HandleAsync(new StockReservedEvent(42, 99.99m, [new OrderLineItem(1, 2)]));

        // Assert
        var authorization = Assert.Single(finished, a => a.OperationName == "authorize payment");
        Assert.Equal(ActivityStatusCode.Error, authorization.Status);
    }

    [Fact]
    public async Task HandleAsync_TagsTheAuthorizationSpanWithTheOrderAndOutcome()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_TagsTheAuthorizationSpanWithTheOrderAndOutcome));
        var sut = BuildHandler(scopeFactory);
        var finished = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = finished.Add
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        await sut.HandleAsync(new StockReservedEvent(42, 99.99m, [new OrderLineItem(1, 2)]));

        // Assert
        var authorization = Assert.Single(finished, a => a.OperationName == "authorize payment");
        Assert.Equal(42, authorization.GetTagItem(OrderPipelineTags.OrderId));
        Assert.Equal("succeeded", authorization.GetTagItem(OrderPipelineTags.Outcome));
    }

    [Fact]
    public async Task HandleAsync_WhenNoDelayIsConfigured_SettlesWithoutWaiting()
    {
        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenNoDelayIsConfigured_SettlesWithoutWaiting));
        var sut = BuildHandler(scopeFactory);

        // Act
        var started = Stopwatch.StartNew();
        await sut.HandleAsync(new StockReservedEvent(42, 99.99m, [new OrderLineItem(1, 2)]));
        started.Stop();

        // Assert
        Assert.True(started.ElapsedMilliseconds < 200, $"took {started.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task HandleAsync_WhenADelayIsConfigured_WaitsBeforeSettling()
    {
        // The demo relies on this pause: without it the saga finishes before the order
        // page has rendered and the status timeline is never seen moving.

        // Arrange
        _paymentSimulator.ShouldFail().Returns(false);
        var scopeFactory = BuildScopeFactory(nameof(HandleAsync_WhenADelayIsConfigured_WaitsBeforeSettling));
        var sut = BuildHandler(scopeFactory, processingDelayMs: 300);

        // Act
        var started = Stopwatch.StartNew();
        await sut.HandleAsync(new StockReservedEvent(42, 99.99m, [new OrderLineItem(1, 2)]));
        started.Stop();

        // Assert
        Assert.True(started.ElapsedMilliseconds >= 300, $"took only {started.ElapsedMilliseconds}ms");
        _eventsPublisher.Received(1).PublishPaymentSucceeded(42);
    }
}
