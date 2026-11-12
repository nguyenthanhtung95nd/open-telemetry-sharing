using Inventory.Api.Common;
using Inventory.Api.Domain;

namespace Inventory.Api.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Result<Product>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<Product>> CreateAsync(SaveProductRequest request, CancellationToken ct = default);
    Task<Result<Product>> UpdateAsync(int id, SaveProductRequest request, CancellationToken ct = default);
    Task<Result<Product>> DeleteAsync(int id, CancellationToken ct = default);
}
