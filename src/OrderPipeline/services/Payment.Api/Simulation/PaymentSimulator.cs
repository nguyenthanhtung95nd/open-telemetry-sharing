using Microsoft.Extensions.Options;

namespace Payment.Api.Simulation;

public sealed class PaymentSimulator(IOptions<PaymentOptions> options, IRandomProvider randomProvider) : IPaymentSimulator
{
    public bool ShouldFail() => randomProvider.NextDouble() < options.Value.FailureRate;
}
