using OrderPipeline.Api.Domain;

namespace OrderPipeline.Api.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
}
