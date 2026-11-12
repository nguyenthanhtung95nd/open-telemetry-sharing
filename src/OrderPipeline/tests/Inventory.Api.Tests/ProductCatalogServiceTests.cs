using Inventory.Api.Domain;
using Inventory.Api.Repositories;
using Inventory.Api.Services;
using NSubstitute;
using Xunit;

namespace Inventory.Api.Tests;

public class ProductCatalogServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ProductCatalogService _sut;

    public ProductCatalogServiceTests()
    {
        _sut = new ProductCatalogService(_repository);
    }

    private static Product Keyboard() =>
        new() { Id = 1, Name = "Keyboard", Price = 49.99m, AvailableQuantity = 10 };

    [Fact]
    public async Task GetAllAsync_ReturnsEveryProduct()
    {
        // Arrange
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Product>
        {
            Keyboard(),
            new() { Id = 2, Name = "Mouse", Price = 19.99m, AvailableQuantity = 5 }
        });

        // Act
        var products = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsSuccess()
    {
        // Arrange
        var product = Keyboard();
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(product, result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductIsUnknown_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsNotPositive_ReturnsFailureWithoutHittingTheDatabase()
    {
        // Act
        var result = await _sut.GetByIdAsync(0);

        // Assert
        Assert.False(result.IsSuccess);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_StoresTheRequestedProduct()
    {
        // Arrange
        var request = new SaveProductRequest("USB-C Dock", 89.00m, 12);
        _repository.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Product>());

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("USB-C Dock", result.Value!.Name);
        Assert.Equal(89.00m, result.Value.Price);
        Assert.Equal(12, result.Value.AvailableQuantity);
    }

    [Fact]
    public async Task CreateAsync_TrimsSurroundingWhitespaceFromTheName()
    {
        // Arrange
        var request = new SaveProductRequest("  Monitor  ", 199.99m, 4);
        _repository.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Product>());

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.Equal("Monitor", result.Value!.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_OverwritesEveryEditableField()
    {
        // Arrange
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Keyboard());
        var request = new SaveProductRequest("Keyboard Pro", 79.99m, 3);

        // Act
        var result = await _sut.UpdateAsync(1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Keyboard Pro", result.Value!.Name);
        Assert.Equal(79.99m, result.Value.Price);
        Assert.Equal(3, result.Value.AvailableQuantity);
        await _repository.Received(1).UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenProductIsUnknown_ReturnsFailureAndWritesNothing()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var result = await _sut.UpdateAsync(999, new SaveProductRequest("Ghost", 1m, 0));

        // Assert
        Assert.False(result.IsSuccess);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_RemovesIt()
    {
        // Arrange
        var product = Keyboard();
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        var result = await _sut.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        await _repository.Received(1).DeleteAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenProductIsUnknown_ReturnsFailureAndWritesNothing()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var result = await _sut.DeleteAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
}
