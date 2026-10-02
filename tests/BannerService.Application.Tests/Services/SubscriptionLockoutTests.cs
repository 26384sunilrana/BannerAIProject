using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class SubscriptionLockoutTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly PasswordHashService _hasher = new();
    private readonly AuthenticationService _service;
    private readonly User _user;
    private readonly Guid _shopId = Guid.NewGuid();

    public SubscriptionLockoutTests()
    {
        _user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "owner@example.com",
            ShopId = _shopId.ToString(),
            PasswordHash = _hasher.HashPassword("Password123!")
        };
        _users.Setup(r => r.GetByEmailAsync("owner@example.com")).ReturnsAsync(_user);
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns(("access", "jwt"));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
        _tokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);

        _service = new AuthenticationService(_users.Object, Mock.Of<IRoleRepository>(), _jwt.Object, _hasher, _tokens.Object,
            Mock.Of<IShopRepository>(), _subscriptions.Object);
    }

    private void SubscriptionIs(SubscriptionStatus? status) =>
        _subscriptions.Setup(r => r.GetByShopIdAsync(_shopId))
            .ReturnsAsync(status == null ? null : new Subscription { ShopId = _shopId, Status = status.Value });

    private Task<(bool success, string message, BannerService.Application.DTOs.AuthTokenDto? tokens)> Login() =>
        _service.LoginAsync(new BannerService.Application.DTOs.LoginDto { Email = "owner@example.com", Password = "Password123!" });

    [Theory]
    [InlineData(SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Suspended)]
    public async Task Login_IsRefused_WhenTheSubscriptionHasEnded(SubscriptionStatus status)
    {
        SubscriptionIs(status);

        var (success, message, tokens) = await Login();

        Assert.False(success);
        Assert.Null(tokens);
        Assert.Equal(AuthenticationService.SubscriptionEndedMessage, message);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Trial)]
    [InlineData(SubscriptionStatus.GracePeriod)]   // logins still work during the grace week
    [InlineData(SubscriptionStatus.RenewalPending)]
    public async Task Login_Works_WhileTheSubscriptionIsRunningOrInGrace(SubscriptionStatus status)
    {
        SubscriptionIs(status);

        Assert.True((await Login()).success);
    }

    [Fact]
    public async Task Login_Works_ForAShopThatHasNotSubscribedYet()
    {
        SubscriptionIs(null);

        Assert.True((await Login()).success);
    }

    [Fact]
    public async Task PlatformAdmins_AreNeverLockedOut()
    {
        SubscriptionIs(SubscriptionStatus.Expired);
        _user.UserRoles.Add(new UserRole { UserId = _user.Id, RoleId = "1", Role = new Role { Id = "1", Name = Role.Admin } });

        Assert.True((await Login()).success);
    }

    [Fact]
    public async Task WrongPassword_DoesNotRevealTheSubscriptionState()
    {
        SubscriptionIs(SubscriptionStatus.Expired);

        var result = await _service.LoginAsync(new BannerService.Application.DTOs.LoginDto { Email = "owner@example.com", Password = "wrong-password" });

        Assert.Equal("Invalid email or password", result.message);
    }

    [Fact]
    public async Task RefreshingASession_IsRefused_OnceTheSubscriptionHasEnded()
    {
        _tokens.Setup(r => r.GetByTokenAsync("old")).ReturnsAsync(new RefreshToken { UserId = _user.Id, Token = "old", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        SubscriptionIs(SubscriptionStatus.Expired);

        var (success, message, _) = await _service.RefreshTokenAsync("old", string.Empty);

        Assert.False(success);
        Assert.Equal(AuthenticationService.SubscriptionEndedMessage, message);
    }
}
