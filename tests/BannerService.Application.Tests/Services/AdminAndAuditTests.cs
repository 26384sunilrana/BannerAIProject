using System.Security.Claims;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Presentation.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class UserAdminServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _tokens = new();
    private readonly UserAdminService _service;
    private readonly User _user = new() { Id = Guid.NewGuid().ToString(), Email = "a@example.com", FirstName = "A", LastName = "B" };

    public UserAdminServiceTests()
    {
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _service = new UserAdminService(_users.Object, _tokens.Object);
    }

    [Fact]
    public async Task Deactivate_DisablesUserAndRevokesRefreshTokens()
    {
        var token = new RefreshToken { UserId = _user.Id, Token = "t" };
        _tokens.Setup(r => r.GetActiveByUserIdAsync(_user.Id)).ReturnsAsync(new List<RefreshToken> { token });

        var result = await _service.DeactivateAsync(_user.Id, Guid.NewGuid().ToString());

        Assert.False(result.IsActive);
        Assert.True(token.IsRevoked);
        _tokens.Verify(r => r.UpdateAsync(token), Times.Once);
    }

    [Fact]
    public async Task Deactivate_OwnAccount_IsRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeactivateAsync(_user.Id, _user.Id));
    }

    [Fact]
    public async Task Activate_And_Unlock_RestoreAccess()
    {
        _user.IsActive = false;
        _user.LockOut();

        Assert.True((await _service.ActivateAsync(_user.Id)).IsActive);
        Assert.False((await _service.UnlockAsync(_user.Id)).IsLockedOut);
    }

    [Fact]
    public async Task UnknownUser_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAsync("missing"));
    }

    [Fact]
    public async Task List_ClampsPageSize()
    {
        _users.Setup(r => r.GetPagedAsync(null, null, false, 1, UserAdminService.MaxPageSize))
            .ReturnsAsync((new List<User> { _user }, 1));

        var page = await _service.ListAsync(null, null, false, 0, 5000);

        Assert.Equal(1, page.Page);
        Assert.Equal(UserAdminService.MaxPageSize, page.PageSize);
        Assert.Single(page.Items);
    }
}

public class AuditLoggingMiddlewareTests
{
    private readonly Mock<IAuditLogRepository> _repository = new();
    private readonly List<AuditLog> _written = new();
    private readonly IServiceScopeFactory _scopes;

    public AuditLoggingMiddlewareTests()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Callback<AuditLog>(_written.Add).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(_repository.Object);
        _scopes = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private AuditLoggingMiddleware Create(RequestDelegate next) =>
        new(next, NullLogger<AuditLoggingMiddleware>.Instance, _scopes);

    private static DefaultHttpContext Context(string path, string method = "GET", bool signedIn = false, Guid? shop = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        if (signedIn)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u1"), new(ClaimTypes.Email, "u1@example.com") };
            if (shop.HasValue) claims.Add(new Claim("shop_id", shop.Value.ToString()));
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }
        return context;
    }

    [Fact]
    public async Task SignedInRequest_IsLoggedWithUserShopAndStatus()
    {
        var shop = Guid.NewGuid();
        var context = Context("/api/banners", "POST", signedIn: true, shop: shop);

        await Create(c => { c.Response.StatusCode = 201; return Task.CompletedTask; }).InvokeAsync(context);

        var entry = Assert.Single(_written);
        Assert.Equal("u1", entry.UserId);
        Assert.Equal(shop, entry.ShopId);
        Assert.Equal("POST", entry.Method);
        Assert.Equal("/api/banners", entry.Path);
        Assert.Equal(201, entry.StatusCode);
    }

    [Fact]
    public async Task AnonymousLoginAttempt_IsLogged_ButOtherAnonymousCallsAreNot()
    {
        await Create(c => { c.Response.StatusCode = 401; return Task.CompletedTask; }).InvokeAsync(Context("/api/authentication/login", "POST"));
        await Create(_ => Task.CompletedTask).InvokeAsync(Context("/api/subscriptions/plans"));

        var entry = Assert.Single(_written);
        Assert.Equal(401, entry.StatusCode);
        Assert.Null(entry.UserId);
    }

    [Fact]
    public async Task HealthAndSwagger_AreNotLogged()
    {
        await Create(_ => Task.CompletedTask).InvokeAsync(Context("/health", signedIn: true));
        await Create(_ => Task.CompletedTask).InvokeAsync(Context("/swagger/index.html", signedIn: true));

        Assert.Empty(_written);
    }

    [Fact]
    public async Task FailingRequest_IsLoggedAs500_AndTheExceptionStillPropagates()
    {
        var context = Context("/api/banners", signedIn: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(_ => throw new InvalidOperationException("boom")).InvokeAsync(context));

        Assert.Equal(500, Assert.Single(_written).StatusCode);
    }

    [Fact]
    public async Task AuditFailure_NeverBreaksTheRequest()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ThrowsAsync(new Exception("db down"));
        var called = false;

        await Create(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(Context("/api/banners", signedIn: true));

        Assert.True(called);
    }
}
