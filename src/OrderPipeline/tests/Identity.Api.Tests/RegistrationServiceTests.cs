using Identity.Api.Common;
using Identity.Api.Keycloak;
using Identity.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Identity.Api.Tests;

public class RegistrationServiceTests
{
    private readonly IKeycloakUserClient _users = Substitute.For<IKeycloakUserClient>();
    private readonly IKeycloakRoleClient _roles = Substitute.For<IKeycloakRoleClient>();
    private readonly RegistrationService _sut;

    public RegistrationServiceTests()
    {
        _sut = new RegistrationService(_users, _roles, NullLogger<RegistrationService>.Instance);
        _roles.GrantAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(call.Arg<Guid>()));
    }

    [Fact]
    public async Task RegisterAsync_WhenRequestIsValid_ReturnsSuccessWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RegisterUserRequest("new.user@example.com", "SecurePass123", "New User");
        _users.CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(userId));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyRegistered_ReturnsFailureFromKeycloak()
    {
        // Arrange
        var request = new RegisterUserRequest("existing@example.com", "SecurePass123", "Existing User");
        _users.CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>("This email is already registered."));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already registered", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task RegisterAsync_WhenEmailIsInvalid_ReturnsFailureWithoutCallingKeycloak(string invalidEmail)
    {
        // Arrange
        var request = new RegisterUserRequest(invalidEmail, "SecurePass123", "Some User");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        await _users.DidNotReceive()
            .CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordIsMissing_ReturnsFailureWithoutCallingKeycloak()
    {
        // Arrange
        var request = new RegisterUserRequest("valid@example.com", "", "Some User");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        await _users.DidNotReceive()
            .CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_GrantsTheCustomerRoleAndNothingElse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _users.CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(userId));

        // Act
        await _sut.RegisterAsync(new RegisterUserRequest("new.user@example.com", "SecurePass123", "New User"));

        // Assert
        await _roles.Received(1).GrantAsync(
            userId,
            Arg.Is<IReadOnlyCollection<string>>(r => r.Single() == "customer"),
            Arg.Any<CancellationToken>());
    }
}
