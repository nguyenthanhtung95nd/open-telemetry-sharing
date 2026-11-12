using Inventory.Api.Data;
using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Repositories;

public sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Products.AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Update(product);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Remove(product);
        await context.SaveChangesAsync(ct);
    }
}
