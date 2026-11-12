using Identity.Api.Common;
using Identity.Api.Keycloak;
using Identity.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Security;
using Xunit;

namespace Identity.Api.Tests;

public class UserAdminServiceTests
{
    private readonly IKeycloakUserClient _users = Substitute.For<IKeycloakUserClient>();
    private readonly IKeycloakRoleClient _roles = Substitute.For<IKeycloakRoleClient>();
    private readonly UserAdminService _sut;

    private static readonly Guid Admin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Customer = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public UserAdminServiceTests()
    {
        _sut = new UserAdminService(_users, _roles, NullLogger<UserAdminService>.Instance);
        _roles.GrantAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(call.Arg<Guid>()));
        _roles.RevokeAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(call.Arg<Guid>()));
    }

    [Fact]
    public async Task ListAsync_AttachesTheRolesEachAccountHolds()
    {
        // Arrange
        _users.ListAsync(Arg.Any<CancellationToken>()).Returns(
            Result.Success<IReadOnlyList<KeycloakAccount>>(
            [
                new(Admin, "admin@example.com", "Platform Admin", true),
                new(Customer, "tung@example.com", "Tung Nguyen", true)
            ]));
        _roles.GetMembersAsync(RealmRoles.Customer, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlySet<Guid>>(new HashSet<Guid> { Admin, Customer }));
        _roles.GetMembersAsync(RealmRoles.Admin, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlySet<Guid>>(new HashSet<Guid> { Admin }));

        // Act
        var result = await _sut.ListAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["customer", "admin"], result.Value!.Single(u => u.Id == Admin).Roles);
        Assert.Equal(["customer"], result.Value!.Single(u => u.Id == Customer).Roles);
    }

    [Fact]
    public async Task ListAsync_DoesNotScaleRoleLookupsWithTheNumberOfAccounts()
    {
        // Arrange
        var many = Enumerable.Range(1, 50)
            .Select(i => new KeycloakAccount(Guid.NewGuid(), $"user{i}@example.com", $"User {i}", true))
            .ToList();
        _users.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeycloakAccount>>(many));
        _roles.GetMembersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlySet<Guid>>(new HashSet<Guid>()));

        // Act
        await _sut.ListAsync();

        // Assert — one lookup per managed role, not per account
        await _roles.Received(2).GetMembersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_GrantsTheRequestedRoles()
    {
        // Arrange
        _users.CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Customer));
        var request = new CreateUserRequest("new@example.com", "New User", "password123", ["customer", "admin"]);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        await _roles.Received(1).GrantAsync(
            Customer,
            Arg.Is<IReadOnlyCollection<string>>(r => r.Contains("customer") && r.Contains("admin")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenARoleIsNotManagedHere_RefusesWithoutCreatingAnything()
    {
        // Act
        var result = await _sut.CreateAsync(
            new CreateUserRequest("new@example.com", "New User", "password123", ["offline_access"]));

        // Assert
        Assert.False(result.IsSuccess);
        await _users.DidNotReceive().CreateAsync(Arg.Any<CreateKeycloakUserRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetRolesAsync_GrantsWhatIsMissingAndRevokesWhatIsNoLongerWanted()
    {
        // Arrange
        _roles.GetUserRolesAsync(Customer, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlySet<string>>(new HashSet<string> { "customer" }));

        // Act
        var result = await _sut.SetRolesAsync(Admin, Customer, new SetRolesRequest(["admin"]));

        // Assert
        Assert.True(result.IsSuccess);
        await _roles.Received(1).GrantAsync(
            Customer, Arg.Is<IReadOnlyCollection<string>>(r => r.Single() == "admin"), Arg.Any<CancellationToken>());
        await _roles.Received(1).RevokeAsync(
            Customer, Arg.Is<IReadOnlyCollection<string>>(r => r.Single() == "customer"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetRolesAsync_LeavesBuiltInKeycloakRolesAlone()
    {
        // Arrange — the account also carries roles this screen does not manage
        _roles.GetUserRolesAsync(Customer, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlySet<string>>(
                new HashSet<string> { "customer", "offline_access", "uma_authorization" }));

        // Act
        await _sut.SetRolesAsync(Admin, Customer, new SetRolesRequest(["customer"]));

        // Assert
        await _roles.Received(1).RevokeAsync(
            Customer, Arg.Is<IReadOnlyCollection<string>>(r => r.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetRolesAsync_WhenAdminsWouldStripTheirOwnAdminRole_Refuses()
    {
        // Act
        var result = await _sut.SetRolesAsync(Admin, Admin, new SetRolesRequest(["customer"]));

        // Assert
        Assert.False(result.IsSuccess);
        await _roles.DidNotReceive().RevokeAsync(
            Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetRolesAsync_WhenAdminsKeepTheirOwnAdminRole_IsAllowed()
    {
        // Arrange
        _roles.GetUserRolesAsync(Admin, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlySet<string>>(new HashSet<string> { "admin" }));

        // Act
        var result = await _sut.SetRolesAsync(Admin, Admin, new SetRolesRequest(["admin", "customer"]));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdminsTargetTheirOwnAccount_Refuses()
    {
        // Act
        var result = await _sut.DeleteAsync(Admin, Admin);

        // Assert
        Assert.False(result.IsSuccess);
        await _users.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetIsSomebodyElse_RemovesTheAccount()
    {
        // Arrange
        _users.DeleteAsync(Customer, Arg.Any<CancellationToken>()).Returns(Result.Success(Customer));

        // Act
        var result = await _sut.DeleteAsync(Admin, Customer);

        // Assert
        Assert.True(result.IsSuccess);
        await _users.Received(1).DeleteAsync(Customer, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableAsync_WhenAdminsTargetTheirOwnAccount_Refuses()
    {
        // Act
        var result = await _sut.DisableAsync(Admin, Admin);

        // Assert
        Assert.False(result.IsSuccess);
        await _users.DidNotReceive().SetEnabledAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableAsync_NeedsNoSelfCheckBecauseItCannotLockAnybodyOut()
    {
        // Arrange
        _users.SetEnabledAsync(Admin, true, Arg.Any<CancellationToken>()).Returns(Result.Success(Admin));

        // Act
        var result = await _sut.EnableAsync(Admin);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
