using BannerService.Application.DTOs;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class OwnerRegistrationTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly AuthenticationService _service;

    private User? _storedUser;
    private Shop? _createdShop;

    public OwnerRegistrationTests()
    {
        _users.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _users.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => _storedUser = u);
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(() => _storedUser);
        _shops.Setup(r => r.CreateAsync(It.IsAny<Shop>())).ReturnsAsync((Shop s) =>
        {
            s.Id = Guid.NewGuid();
            return _createdShop = s;
        });
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns(("access", "jwt-id"));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh");

        _service = new AuthenticationService(
            _users.Object, _roles.Object, _jwt.Object, new PasswordHashService(), _refreshTokens.Object, _shops.Object,
            Mock.Of<ISubscriptionRepository>());
    }

    private static RegisterDto Request() => new()
    {
        Email = "Owner@Example.com",
        Password = "Password123!",
        FirstName = "Olive",
        LastName = "Owner",
        ShopName = "Olive Mart",
        City = "Pune"
    };

    [Fact]
    public async Task Register_CreatesShopOwnedByNewUser_AndAssignsOwnerRole()
    {
        var (success, _, tokens) = await _service.RegisterAsync(Request());

        Assert.True(success);
        Assert.NotNull(tokens);
        Assert.NotNull(_createdShop);
        Assert.Equal("Olive Mart", _createdShop!.Name);
        Assert.Equal(Guid.Parse(_storedUser!.Id), _createdShop.OwnerUserId);
        Assert.Equal(_createdShop.Id.ToString(), _storedUser.ShopId);
        _roles.Verify(r => r.AssignRoleAsync(_storedUser.Id, "2"), Times.Once);
        _roles.Verify(r => r.AssignRoleAsync(It.IsAny<string>(), "3"), Times.Never);
    }

    [Fact]
    public async Task Register_NormalisesEmail()
    {
        await _service.RegisterAsync(Request());

        Assert.Equal("owner@example.com", _storedUser!.Email);
    }

    [Fact]
    public async Task Register_WithoutShopName_IsRejected()
    {
        var request = Request();
        request.ShopName = "  ";

        var (success, message, _) = await _service.RegisterAsync(request);

        Assert.False(success);
        Assert.Contains("Shop name", message);
        _users.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithExistingShopId_IsRejected_SoNobodyCanJoinAnotherShop()
    {
        var request = Request();
        request.ShopId = Guid.NewGuid().ToString();

        var (success, _, _) = await _service.RegisterAsync(request);

        Assert.False(success);
        _users.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_WhenShopCreationFails_RemovesTheUser()
    {
        _shops.Setup(r => r.CreateAsync(It.IsAny<Shop>())).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegisterAsync(Request()));

        _users.Verify(r => r.DeleteAsync(_storedUser!.Id), Times.Once);
    }
}
