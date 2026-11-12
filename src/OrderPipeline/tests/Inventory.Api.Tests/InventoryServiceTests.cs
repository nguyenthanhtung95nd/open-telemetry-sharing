using Inventory.Api.Domain;
using Inventory.Api.Repositories;
using Inventory.Api.Services;
using NSubstitute;
using Xunit;

namespace Inventory.Api.Tests;

public class InventoryServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _sut = new InventoryService(_repository);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WhenStockCoversRequest_ReportsAvailable()
    {
        // Arrange
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Product { Id = 1, Name = "Keyboard", Price = 49.99m, AvailableQuantity = 10 });

        // Act
        var result = await _sut.CheckAvailabilityAsync(1, 4);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsAvailable);
        Assert.Equal(10, result.Value.AvailableQuantity);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WhenStockIsShort_ReportsUnavailable()
    {
        // Arrange
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Product { Id = 1, Name = "Keyboard", Price = 49.99m, AvailableQuantity = 2 });

        // Act
        var result = await _sut.CheckAvailabilityAsync(1, 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsAvailable);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReportsCatalogPriceSoCallersNeedNoSecondCall()
    {
        // Arrange
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Product { Id = 1, Name = "Keyboard", Price = 49.99m, AvailableQuantity = 10 });

        // Act
        var result = await _sut.CheckAvailabilityAsync(1, 1);

        // Assert
        Assert.Equal(49.99m, result.Value!.UnitPrice);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WhenProductIsUnknown_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var result = await _sut.CheckAvailabilityAsync(404, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

}
