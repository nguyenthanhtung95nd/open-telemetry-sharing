using OrderPipeline.Api.Logging;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace OrderPipeline.Api.Repositories;

public sealed class OrderRepository(AppDbContext dbContext, ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await dbContext.Orders.AddAsync(order, ct);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.OrderPersistenceFailed(ex, order.Id);
            throw;
        }
    }
}
