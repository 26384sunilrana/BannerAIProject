using BannerService.Application.DTOs;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class MyApprovalRoleTests
{
    private readonly Mock<IShopRepository> _shops = new();
    private readonly ShopTeamService _service;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _executiveId = Guid.NewGuid();
    private readonly Shop _shop;

    public MyApprovalRoleTests()
    {
        _shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", OwnerUserId = _ownerId };
        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _service = new ShopTeamService(_shops.Object, Mock.Of<IUserRepository>(), Mock.Of<ISubscriptionRepository>(),
            Mock.Of<IRoleRepository>(), new PasswordHashService());
    }

    [Fact]
    public async Task Owner_IsOwnerAndApprover()
    {
        var role = await _service.GetMyApprovalRoleAsync(_shop.Id, _ownerId);

        Assert.True(role.IsOwner);
        Assert.True(role.CanApprove);
    }

    [Fact]
    public async Task Executive_CanApproveOnlyWhenDesignated()
    {
        Assert.False((await _service.GetMyApprovalRoleAsync(_shop.Id, _executiveId)).CanApprove);

        _shop.SetApprovers(true, new[] { _executiveId.ToString() });
        var role = await _service.GetMyApprovalRoleAsync(_shop.Id, _executiveId);

        Assert.False(role.IsOwner);
        Assert.True(role.CanApprove);
    }

    [Fact]
    public async Task UnknownShop_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetMyApprovalRoleAsync(Guid.NewGuid(), _ownerId));
    }
}

public class AnonymousRefreshTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly AuthenticationService _service;
    private readonly User _user = new() { Id = Guid.NewGuid().ToString(), Email = "a@example.com" };
    private readonly RefreshToken _stored;

    public AnonymousRefreshTests()
    {
        _stored = new RefreshToken { UserId = _user.Id, Token = "old", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _tokens.Setup(r => r.GetByTokenAsync("old")).ReturnsAsync(_stored);
        _tokens.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _tokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        _jwt.Setup(j => j.GenerateAccessToken(_user)).Returns(("new-access", "jwt"));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh");

        _service = new AuthenticationService(_users.Object, Mock.Of<IRoleRepository>(), _jwt.Object,
            new PasswordHashService(), _tokens.Object, Mock.Of<IShopRepository>(), Mock.Of<ISubscriptionRepository>());
    }

    [Fact]
    public async Task ExpiredAccessToken_RefreshWorksWithoutAUserId()
    {
        var (success, _, tokens) = await _service.RefreshTokenAsync("old", string.Empty);

        Assert.True(success);
        Assert.Equal("new-access", tokens!.AccessToken);
        Assert.True(_stored.IsRevoked);
    }

    [Fact]
    public async Task SuppliedUserId_MustStillMatchTheToken()
    {
        var (success, _, _) = await _service.RefreshTokenAsync("old", Guid.NewGuid().ToString());

        Assert.False(success);
    }

    [Fact]
    public async Task UnknownOrRevokedToken_IsRejected()
    {
        _stored.Revoke();

        Assert.False((await _service.RefreshTokenAsync("old", string.Empty)).success);
        Assert.False((await _service.RefreshTokenAsync("nope", string.Empty)).success);
    }
}
