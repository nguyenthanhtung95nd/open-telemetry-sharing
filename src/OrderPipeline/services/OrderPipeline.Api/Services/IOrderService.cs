using OrderPipeline.Api.Common;
using OrderPipeline.Api.Domain;
using OrderPipeline.Api.Security;

namespace OrderPipeline.Api.Services;

public interface IOrderService
{
    Task<Result<Order>> GetByIdAsync(int id, AuthenticatedUser user, CancellationToken ct = default);
    Task<Result<Order>> CreateAsync(CreateOrderRequest request, AuthenticatedUser user, CancellationToken ct = default);
}
