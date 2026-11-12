using Destructurama.Attributed;

namespace OrderPipeline.Api.Domain;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    [LogMasked(ShowFirst = 3, PreserveLength = true)]
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
