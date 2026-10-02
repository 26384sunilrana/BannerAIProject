using BannerService.Application.DTOs;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly PasswordHashService _hasher = new();
    private readonly AccountService _service;
    private readonly User _user;
    private readonly List<RefreshToken> _active = new();

    public AccountServiceTests()
    {
        _user = new User { Id = Guid.NewGuid().ToString(), Email = "sam@example.com", FirstName = "Sam", LastName = "Seller", ShopId = Guid.NewGuid().ToString(), PasswordHash = _hasher.HashPassword("Password123!") };
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _roles.Setup(r => r.GetByUserIdAsync(_user.Id)).ReturnsAsync(new List<Role> { new() { Id = "3", Name = "SalesExecutive" } });
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns(("access", "jwt-id"));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh");
        _tokens.Setup(t => t.GetActiveByUserIdAsync(_user.Id)).ReturnsAsync(() => _active.Where(t => t.IsActive).ToList());
        _tokens.Setup(t => t.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _service = new AccountService(_users.Object, _roles.Object, _tokens.Object, _hasher, _jwt.Object);
    }

    private RefreshToken Session(string token) { var t = new RefreshToken { UserId = _user.Id, Token = token, ExpiresAt = DateTime.UtcNow.AddDays(1) }; _active.Add(t); return t; }

    // ----- details

    [Fact]
    public async Task Get_ReturnsTheDetailsAndRoles()
    {
        var account = await _service.GetAsync(_user.Id);

        Assert.Equal("sam@example.com", account.Email);
        Assert.Equal(new[] { "SalesExecutive" }, account.Roles);
    }

    [Fact]
    public async Task Update_TrimsAndSaves()
    {
        var account = await _service.UpdateAsync(_user.Id, "  Samira ", " Seller ", " +91 98765 43210 ");

        Assert.Equal("Samira", _user.FirstName);
        Assert.Equal("+91 98765 43210", _user.PhoneNumber);
        Assert.Equal("Samira", account.FirstName);
    }

    [Fact]
    public async Task Update_AllowsAnEmptyPhoneNumber_AndClearsIt()
    {
        _user.PhoneNumber = "12345678";

        await _service.UpdateAsync(_user.Id, "Sam", "Seller", "  ");

        Assert.Null(_user.PhoneNumber);
    }

    [Theory]
    [InlineData("", "Seller", null)]
    [InlineData("   ", "Seller", null)]
    [InlineData("Sam", "Seller", "abc")]
    [InlineData("Sam", "Seller", "12")]
    [InlineData("Sam\u0007", "Seller", null)]
    public async Task Update_RefusesWhatIsNotAName_OrNotAPhoneNumber(string first, string last, string? phone)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(_user.Id, first, last, phone));
        _users.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    // ----- password

    [Fact]
    public async Task ChangePassword_SetsTheNewOne_EndsEveryOtherSession_AndStartsOneForThisDevice()
    {
        var other = Session("phone");
        var laptop = Session("laptop");

        var tokens = await _service.ChangePasswordAsync(_user.Id, "Password123!", "BetterPass456!", "BetterPass456!", "10.0.0.1", "Chrome");

        Assert.True(_hasher.VerifyPassword("BetterPass456!", _user.PasswordHash));
        Assert.False(_hasher.VerifyPassword("Password123!", _user.PasswordHash));
        Assert.True(other.IsRevoked && laptop.IsRevoked);
        Assert.Equal("new-refresh", tokens.RefreshToken);
        Assert.Equal("access", tokens.AccessToken);
        _tokens.Verify(t => t.AddAsync(It.Is<RefreshToken>(r => r.Token == "new-refresh" && r.UserId == _user.Id && r.IpAddress == "10.0.0.1")), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithAWrongCurrentPassword_IsRefused_AndCountsAsAFailedSignIn()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ChangePasswordAsync(_user.Id, "wrong", "BetterPass456!", "BetterPass456!", null, null));

        Assert.Contains("current password", ex.Message);
        Assert.Equal(1, _user.LoginAttempts);
        Assert.True(_hasher.VerifyPassword("Password123!", _user.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_LocksTheAccountAfterFiveWrongGuesses()
    {
        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<ArgumentException>(() => _service.ChangePasswordAsync(_user.Id, "wrong", "BetterPass456!", "BetterPass456!", null, null));

        Assert.True(_user.IsLockedOut);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangePasswordAsync(_user.Id, "Password123!", "BetterPass456!", "BetterPass456!", null, null));
    }

    [Theory]
    [InlineData("short1", "short1", "at least 8")]
    [InlineData("BetterPass456!", "Different789!", "not the same")]
    [InlineData("Password123!", "Password123!", "have not been using")]
    public async Task ChangePassword_RefusesAWeakMismatchedOrUnchangedPassword(string newPassword, string confirm, string expectedPart)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ChangePasswordAsync(_user.Id, "Password123!", newPassword, confirm, null, null));

        Assert.Contains(expectedPart, ex.Message);
        Assert.True(_hasher.VerifyPassword("Password123!", _user.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_ResetsEarlierFailedAttempts()
    {
        _user.LoginAttempts = 3;

        await _service.ChangePasswordAsync(_user.Id, "Password123!", "BetterPass456!", "BetterPass456!", null, null);

        Assert.Equal(0, _user.LoginAttempts);
    }

    // ----- signing out

    [Fact]
    public async Task SignOutEverywhere_EndsEverySession()
    {
        var a = Session("a");
        var b = Session("b");

        var ended = await _service.SignOutEverywhereAsync(_user.Id);

        Assert.Equal(2, ended);
        Assert.True(a.IsRevoked && b.IsRevoked);
    }

    [Fact]
    public async Task UnknownUser_IsNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAsync("nobody"));
    }
}

public class ShopMembershipServiceTests
{
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly PasswordHashService _hasher = new();
    private readonly ShopMembershipService _service;

    private readonly Shop _shop;
    private readonly User _owner;
    private readonly User _sam;
    private readonly User _tess;
    private readonly Dictionary<string, List<string>> _roleNames = new();
    private readonly List<RefreshToken> _sessions = new();

    public ShopMembershipServiceTests()
    {
        var ownerId = Guid.NewGuid();
        _shop = new Shop { Id = Guid.NewGuid(), Name = "Olive Mart", OwnerUserId = ownerId, Status = ShopStatus.Active };
        _owner = AUser(ownerId.ToString(), "owner@example.com", "ShopOwner");
        _sam = AUser(Guid.NewGuid().ToString(), "sam@example.com", "SalesExecutive");
        _tess = AUser(Guid.NewGuid().ToString(), "tess@example.com", "SalesExecutive");
        _shop.SetApprovers(true, new List<string> { _sam.Id });

        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _shops.Setup(r => r.UpdateAsync(It.IsAny<Shop>())).ReturnsAsync((Shop s) => s);
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _users.Setup(r => r.GetByShopIdAsync(_shop.Id.ToString())).ReturnsAsync(() => AllUsers.Where(u => u.ShopId == _shop.Id.ToString() && u.IsActive).ToList());
        _roles.Setup(r => r.GetByUserIdAsync(It.IsAny<string>())).ReturnsAsync((string id) => _roleNames.GetValueOrDefault(id, new()).Select(n => new Role { Name = n }).ToList());
        _roles.Setup(r => r.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string id, string roleId) => { _roleNames[id].Remove(Name(roleId)); return true; });
        _roles.Setup(r => r.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string id, string roleId) => { _roleNames[id].Add(Name(roleId)); return null!; });
        _tokens.Setup(t => t.GetActiveByUserIdAsync(It.IsAny<string>())).ReturnsAsync((string id) => _sessions.Where(s => s.UserId == id && s.IsActive).ToList());
        _tokens.Setup(t => t.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _subscriptions.Setup(r => r.GetByShopIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Subscription { Status = SubscriptionStatus.Active });

        _service = new ShopMembershipService(_shops.Object, _users.Object, _roles.Object, _tokens.Object, _subscriptions.Object, _hasher);
    }

    private static string Name(string roleId) => roleId switch { "2" => "ShopOwner", "3" => "SalesExecutive", _ => "Admin" };

    private readonly List<User> _all = new();
    private IEnumerable<User> AllUsers => _all;

    private User AUser(string id, string email, string role)
    {
        var user = new User { Id = id, Email = email, ShopId = _shop.Id.ToString(), IsActive = true, PasswordHash = _hasher.HashPassword("Password123!") };
        _roleNames[id] = new List<string> { role };
        _all.Add(user);
        _sessions.Add(new RefreshToken { UserId = id, Token = "t-" + email, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        _users.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
        return user;
    }

    private bool SessionEnded(User user) => _sessions.Single(s => s.UserId == user.Id).IsRevoked;

    // ----- owner hands the shop over

    [Fact]
    public async Task Transfer_SwapsTheRoles_UpdatesTheShop_AndEndsBothPeoplesSessions()
    {
        var result = await _service.TransferOwnershipAsync(_shop.Id, _owner.Id, _sam.Id, "Password123!");

        Assert.Equal(_sam.Id, _shop.OwnerUserId!.Value.ToString());
        Assert.Equal(new[] { "ShopOwner" }, _roleNames[_sam.Id]);
        Assert.Equal(new[] { "SalesExecutive" }, _roleNames[_owner.Id]);
        Assert.Equal(_owner.Id, result.PreviousOwnerUserId);
        Assert.True(SessionEnded(_sam) && SessionEnded(_owner));
        Assert.False(SessionEnded(_tess));
    }

    [Fact]
    public async Task Transfer_TheNewOwnerIsNoLongerListedAsAnExecutiveApprover()
    {
        await _service.TransferOwnershipAsync(_shop.Id, _owner.Id, _sam.Id, "Password123!");

        Assert.DoesNotContain(_sam.Id, _shop.ApproverUserIds);
    }

    [Fact]
    public async Task Transfer_WithAWrongPassword_ChangesNothing_AndCountsAsAFailedSignIn()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.TransferOwnershipAsync(_shop.Id, _owner.Id, _sam.Id, "nope"));

        Assert.Equal(_owner.Id, _shop.OwnerUserId!.Value.ToString());
        Assert.Equal(1, _owner.LoginAttempts);
        Assert.False(SessionEnded(_sam));
    }

    [Fact]
    public async Task Transfer_ByAnExecutive_IsForbidden()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.TransferOwnershipAsync(_shop.Id, _sam.Id, _tess.Id, "Password123!"));
    }

    [Fact]
    public async Task Transfer_ToSomeoneOutsideTheShop_OrAnInactiveLogin_OrTheOwner_IsRefused()
    {
        var stranger = AUser(Guid.NewGuid().ToString(), "stranger@example.com", "SalesExecutive");
        stranger.ShopId = Guid.NewGuid().ToString();
        _tess.IsActive = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TransferOwnershipAsync(_shop.Id, _owner.Id, stranger.Id, "Password123!"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TransferOwnershipAsync(_shop.Id, _owner.Id, _tess.Id, "Password123!"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TransferOwnershipAsync(_shop.Id, _owner.Id, _owner.Id, "Password123!"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.TransferOwnershipAsync(_shop.Id, _owner.Id, "missing", "Password123!"));
    }

    [Fact]
    public async Task Transfer_ToAnAdministrator_IsRefused()
    {
        var admin = AUser(Guid.NewGuid().ToString(), "admin@example.com", "Admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TransferOwnershipAsync(_shop.Id, _owner.Id, admin.Id, "Password123!"));
    }

    // ----- administrator

    [Fact]
    public async Task AdminAssign_NeedsNoPassword_AndWorksForAShopWithoutAnOwner()
    {
        _shop.OwnerUserId = null;

        var result = await _service.AssignOwnerAsAdminAsync(_shop.Id, _tess.Id);

        Assert.Equal(_tess.Id, _shop.OwnerUserId!.Value.ToString());
        Assert.Null(result.PreviousOwnerUserId);
        Assert.Equal(new[] { "ShopOwner" }, _roleNames[_tess.Id]);
    }

    // ----- moving an executive

    private Shop AnotherShop(int executives = 0, SubscriptionStatus? subscription = SubscriptionStatus.Active, ShopStatus status = ShopStatus.Active)
    {
        var other = new Shop { Id = Guid.NewGuid(), Name = "Other Mart", OwnerUserId = Guid.NewGuid(), Status = status };
        _shops.Setup(r => r.GetByIdAsync(other.Id)).ReturnsAsync(other);
        _subscriptions.Setup(r => r.GetByShopIdAsync(other.Id)).ReturnsAsync(subscription == null ? null : new Subscription { Status = subscription.Value });
        var members = new List<User>();
        for (var i = 0; i < executives; i++) members.Add(new User { Id = Guid.NewGuid().ToString(), ShopId = other.Id.ToString(), IsActive = true });
        _users.Setup(r => r.GetByShopIdAsync(other.Id.ToString())).ReturnsAsync(members);
        return other;
    }

    [Fact]
    public async Task Move_ChangesTheShop_DropsTheApproverRole_AndEndsTheSessions()
    {
        var other = AnotherShop(executives: 1);

        var result = await _service.MoveExecutiveAsync(_sam.Id, other.Id);

        Assert.Equal(other.Id.ToString(), _sam.ShopId);
        Assert.Equal(_shop.Id, result.FromShopId);
        Assert.DoesNotContain(_sam.Id, _shop.ApproverUserIds);
        Assert.True(SessionEnded(_sam));
        Assert.False(SessionEnded(_tess));
    }

    [Fact]
    public async Task Move_IsRefusedWhenTheTargetIsFullUnsubscribedOrNotActive()
    {
        var full = AnotherShop(executives: 2);
        var unsubscribed = AnotherShop(subscription: null);
        var ended = AnotherShop(subscription: SubscriptionStatus.Expired);
        var archived = AnotherShop(status: ShopStatus.Archived);

        foreach (var target in new[] { full, unsubscribed, ended, archived })
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MoveExecutiveAsync(_sam.Id, target.Id));

        Assert.Equal(_shop.Id.ToString(), _sam.ShopId);
    }

    [Fact]
    public async Task Move_IsRefusedForOwnersAdministratorsAndTheSameShop()
    {
        var other = AnotherShop();
        var admin = AUser(Guid.NewGuid().ToString(), "admin@example.com", "Admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MoveExecutiveAsync(_owner.Id, other.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MoveExecutiveAsync(admin.Id, other.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MoveExecutiveAsync(_sam.Id, _shop.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.MoveExecutiveAsync(_sam.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Move_IsRefusedForASwitchedOffLogin()
    {
        var other = AnotherShop();
        _sam.IsActive = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MoveExecutiveAsync(_sam.Id, other.Id));
    }
}

public class RefreshGraceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly AuthenticationService _service;
    private readonly User _user = new() { Id = Guid.NewGuid().ToString(), Email = "owner@example.com", IsActive = true, ShopId = Guid.NewGuid().ToString() };

    public RefreshGraceTests()
    {
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns(("fresh-access", "jwt"));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("rotated");
        _tokens.Setup(t => t.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken t) => t);
        _service = new AuthenticationService(_users.Object, Mock.Of<IRoleRepository>(), _jwt.Object, new PasswordHashService(), _tokens.Object,
            Mock.Of<IShopRepository>(), Mock.Of<ISubscriptionRepository>());
    }

    private RefreshToken Stored(string token, bool revoked = false, string? replacedBy = null, TimeSpan? revokedAgo = null)
    {
        var t = new RefreshToken { UserId = _user.Id, Token = token, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        if (revoked)
        {
            t.Revoke(replacedBy);
            t.RevokedAt = DateTime.UtcNow - (revokedAgo ?? TimeSpan.Zero);
        }
        _tokens.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(t);
        return t;
    }

    [Fact]
    public async Task ARefreshTokenJustReplaced_StillWorks_AndHandsBackTheSameReplacement()
    {
        Stored("old", revoked: true, replacedBy: "new", revokedAgo: TimeSpan.FromSeconds(5));
        Stored("new");

        var (success, _, tokens) = await _service.RefreshTokenAsync("old", string.Empty);

        Assert.True(success);
        Assert.Equal("new", tokens!.RefreshToken);
        Assert.Equal("fresh-access", tokens.AccessToken);
        _tokens.Verify(t => t.AddAsync(It.IsAny<RefreshToken>()), Times.Never); // nothing was rotated again
    }

    [Fact]
    public async Task ARefreshTokenReplacedLongAgo_IsRefused()
    {
        Stored("old", revoked: true, replacedBy: "new", revokedAgo: TimeSpan.FromMinutes(5));
        Stored("new");

        var (success, message, _) = await _service.RefreshTokenAsync("old", string.Empty);

        Assert.False(success);
        Assert.Equal("Invalid refresh token", message);
    }

    [Fact]
    public async Task ARevokedTokenWithNoReplacement_IsRefused_EvenRightAfterTheLogout()
    {
        Stored("old", revoked: true, replacedBy: null, revokedAgo: TimeSpan.FromSeconds(1));

        Assert.False((await _service.RefreshTokenAsync("old", string.Empty)).success);
    }

    [Fact]
    public async Task TheGraceWindowDoesNotWorkForAnotherUser()
    {
        Stored("old", revoked: true, replacedBy: "new", revokedAgo: TimeSpan.FromSeconds(5));
        Stored("new");

        Assert.False((await _service.RefreshTokenAsync("old", Guid.NewGuid().ToString())).success);
    }

    [Fact]
    public async Task AnActiveTokenRotatesAsBefore()
    {
        var current = Stored("current");

        var (success, _, tokens) = await _service.RefreshTokenAsync("current", string.Empty);

        Assert.True(success);
        Assert.Equal("rotated", tokens!.RefreshToken);
        Assert.True(current.IsRevoked);
        Assert.Equal("rotated", current.ReplacedByToken);
    }

    [Fact]
    public async Task RevokeByTokenAlone_EndsThatSession_AndIsQuietForAnUnknownToken()
    {
        var token = Stored("mine");
        _tokens.Setup(r => r.GetByTokenAsync("unknown")).ReturnsAsync((RefreshToken?)null);

        Assert.True(await _service.RevokeTokenAsync("mine"));
        Assert.True(token.IsRevoked);
        Assert.False(await _service.RevokeTokenAsync("unknown"));
        Assert.False(await _service.RevokeTokenAsync(""));
    }
}
