using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderPipeline.Api.Clients;
using OrderPipeline.Api.Common;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Messaging;
using OrderPipeline.Api.Repositories;
using OrderPipeline.Api.Security;
using OrderPipeline.Api.Services;
using Xunit;

namespace OrderPipeline.Api.Tests;

public class OrderServiceTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInventoryClient _inventoryClient = Substitute.For<IInventoryClient>();
    private readonly IOrderEventsPublisher _orderEventsPublisher = Substitute.For<IOrderEventsPublisher>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(
            _orderRepository, _inventoryClient, _orderEventsPublisher,
            NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_UsesAuthenticatedUserAsOrderOwner()
    {
        // Arrange
        var user = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var request = new CreateOrderRequest([new CreateOrderItemRequest(1, 2)]);

        _inventoryClient.CheckAvailabilityAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new InventoryAvailability(1, 2, true, 10, 10m)));

        // Act
        var result = await _sut.CreateAsync(request, user);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Name, result.Value!.CustomerName);
        Assert.Equal(user.Email, result.Value!.CustomerEmail);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_SnapshotsUnitPriceReportedByInventory()
    {
        // Arrange
        var user = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var request = new CreateOrderRequest([new CreateOrderItemRequest(1, 3)]);

        _inventoryClient.CheckAvailabilityAsync(1, 3, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new InventoryAvailability(1, 3, true, 10, 49.99m)));

        // Act
        var result = await _sut.CreateAsync(request, user);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(49.99m, result.Value!.Items.Single().UnitPrice);
        Assert.Equal(149.97m, result.Value!.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_WhenInventoryDoesNotKnowTheProduct_ReturnsFailure()
    {
        // Arrange
        var user = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var request = new CreateOrderRequest([new CreateOrderItemRequest(404, 1)]);

        _inventoryClient.CheckAvailabilityAsync(404, 1, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<InventoryAvailability>("Product 404 not found in inventory."));

        // Act
        var result = await _sut.CreateAsync(request, user);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenStockIsInsufficient_ReturnsFailure()
    {
        // Arrange
        var user = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var request = new CreateOrderRequest([new CreateOrderItemRequest(1, 50)]);

        _inventoryClient.CheckAvailabilityAsync(1, 50, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new InventoryAvailability(1, 50, false, 10, 10m)));

        // Act
        var result = await _sut.CreateAsync(request, user);

        // Assert
        Assert.False(result.IsSuccess);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenNoItems_ReturnsFailure()
    {
        // Arrange
        var user = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var request = new CreateOrderRequest([]);

        // Act
        var result = await _sut.CreateAsync(request, user);

        // Assert
        Assert.False(result.IsSuccess);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderBelongsToCaller_ReturnsSuccess()
    {
        // Arrange
        var owner = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var order = new Order { Id = 1, CustomerEmail = owner.Email, CustomerName = owner.Name };
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        // Act
        var result = await _sut.GetByIdAsync(1, owner);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(order, result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderBelongsToDifferentUser_ReturnsFailure()
    {
        // Arrange
        var owner = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        var intruder = new AuthenticatedUser("user-sub-2", "intruder@example.com", "Intruder");
        var order = new Order { Id = 1, CustomerEmail = owner.Email, CustomerName = owner.Name };
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        // Act
        var result = await _sut.GetByIdAsync(1, intruder);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderNotFound_ReturnsFailure()
    {
        // Arrange
        var user = new AuthenticatedUser("user-sub-1", "owner@example.com", "Order Owner");
        _orderRepository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Order?)null);

        // Act
        var result = await _sut.GetByIdAsync(999, user);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
