namespace Payment.Api.Simulation;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public double FailureRate { get; init; } = 0.15;

    public int ProcessingDelayMs { get; init; }
}
