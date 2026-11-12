using Microsoft.Extensions.Options;
using NSubstitute;
using Payment.Api.Simulation;
using Xunit;

namespace Payment.Api.Tests;

public class PaymentSimulatorTests
{
    private readonly IRandomProvider _randomProvider = Substitute.For<IRandomProvider>();

    [Fact]
    public void ShouldFail_WhenRandomValueBelowFailureRate_ReturnsTrue()
    {
        // Arrange
        _randomProvider.NextDouble().Returns(0.10);
        var sut = new PaymentSimulator(Options.Create(new PaymentOptions { FailureRate = 0.15 }), _randomProvider);

        // Act
        var result = sut.ShouldFail();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldFail_WhenRandomValueAboveFailureRate_ReturnsFalse()
    {
        // Arrange
        _randomProvider.NextDouble().Returns(0.90);
        var sut = new PaymentSimulator(Options.Create(new PaymentOptions { FailureRate = 0.15 }), _randomProvider);

        // Act
        var result = sut.ShouldFail();

        // Assert
        Assert.False(result);
    }
}
