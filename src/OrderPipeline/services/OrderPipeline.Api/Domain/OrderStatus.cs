namespace OrderPipeline.Api.Domain;

public enum OrderStatus
{
    Pending = 0,
    OutOfStock = 1,
    Paid = 2,
    PaymentFailed = 3,
    StockReserved = 4
}
