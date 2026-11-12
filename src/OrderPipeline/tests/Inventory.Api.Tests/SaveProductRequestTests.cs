using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Inventory.Api.Services;
using Xunit;

namespace Inventory.Api.Tests;

public class SaveProductRequestTests
{
    private static ParameterInfo Parameter(string name) =>
        typeof(SaveProductRequest)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Single(p => p.Name == name);

    private static bool IsRejected(string parameterName, object? value) =>
        Parameter(parameterName)
            .GetCustomAttributes<ValidationAttribute>()
            .Any(attribute => !attribute.IsValid(value));

    [Theory]
    [InlineData(nameof(SaveProductRequest.Name))]
    [InlineData(nameof(SaveProductRequest.Price))]
    [InlineData(nameof(SaveProductRequest.AvailableQuantity))]
    public void EveryField_CarriesValidationOnTheConstructorParameter(string parameterName)
    {
        Assert.NotEmpty(Parameter(parameterName).GetCustomAttributes<ValidationAttribute>());
    }

    [Fact]
    public void NoField_CarriesValidationOnTheGeneratedProperty()
    {
        var offenders = typeof(SaveProductRequest)
            .GetProperties()
            .Where(p => p.GetCustomAttributes<ValidationAttribute>().Any())
            .Select(p => p.Name);

        Assert.Empty(offenders);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Name_WhenBlank_IsRejected(string? name)
    {
        Assert.True(IsRejected(nameof(SaveProductRequest.Name), name));
    }

    [Fact]
    public void Name_WhenSupplied_IsAccepted()
    {
        Assert.False(IsRejected(nameof(SaveProductRequest.Name), "Keyboard"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Price_WhenNotAboveZero_IsRejected(int price)
    {
        Assert.True(IsRejected(nameof(SaveProductRequest.Price), (decimal)price));
    }

    [Fact]
    public void Price_WhenAboveZero_IsAccepted()
    {
        Assert.False(IsRejected(nameof(SaveProductRequest.Price), 49.99m));
    }

    [Fact]
    public void AvailableQuantity_WhenNegative_IsRejected()
    {
        Assert.True(IsRejected(nameof(SaveProductRequest.AvailableQuantity), -1));
    }

    [Fact]
    public void AvailableQuantity_WhenZero_IsAccepted()
    {
        Assert.False(IsRejected(nameof(SaveProductRequest.AvailableQuantity), 0));
    }
}
