namespace Payment.Api.Simulation;

public sealed class RandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();
}
