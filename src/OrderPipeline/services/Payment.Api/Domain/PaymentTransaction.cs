namespace Payment.Api.Domain;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
