using Inventory.Api.Common;
using Inventory.Api.Domain;
using Inventory.Api.Repositories;

namespace Inventory.Api.Services;

public sealed class ProductCatalogService(IProductRepository repository) : IProductCatalogService
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await repository.GetAllAsync(ct);
    }

    public async Task<Result<Product>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (id <= 0) return Result.Failure<Product>("Product ID must be greater than zero.");

        var product = await repository.GetByIdAsync(id, ct);
        return product is null
            ? Result.Failure<Product>($"Product {id} not found.")
            : Result.Success(product);
    }

    public async Task<Result<Product>> CreateAsync(SaveProductRequest request, CancellationToken ct = default)
    {
        var created = await repository.AddAsync(new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            AvailableQuantity = request.AvailableQuantity
        }, ct);

        return Result.Success(created);
    }

    public async Task<Result<Product>> UpdateAsync(int id, SaveProductRequest request, CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(id, ct);
        if (existing is null) return Result.Failure<Product>($"Product {id} not found.");

        existing.Name = request.Name.Trim();
        existing.Price = request.Price;
        existing.AvailableQuantity = request.AvailableQuantity;

        await repository.UpdateAsync(existing, ct);
        return Result.Success(existing);
    }

    public async Task<Result<Product>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(id, ct);
        if (existing is null) return Result.Failure<Product>($"Product {id} not found.");

        await repository.DeleteAsync(existing, ct);
        return Result.Success(existing);
    }
}
