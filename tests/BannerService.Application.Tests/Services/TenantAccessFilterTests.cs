using System.Security.Claims;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Presentation.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class TenantAccessFilterTests
{
    private readonly Mock<IAdvertisementRepository> _ads = new();
    private readonly Mock<IAnalyticsRepository> _analytics = new();
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IPublishWorkflowRepository> _workflows = new();
    private readonly TenantAccessFilter _filter;

    private readonly Guid _myShop = Guid.NewGuid();
    private readonly Guid _otherShop = Guid.NewGuid();
    private readonly Guid _me = Guid.NewGuid();

    public TenantAccessFilterTests()
    {
        _filter = new TenantAccessFilter(_ads.Object, _analytics.Object, _invoices.Object, _subscriptions.Object, _workflows.Object);
    }

    private ClaimsPrincipal User(bool admin = false, bool authenticated = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _me.ToString()),
            new("shop_id", _myShop.ToString())
        };
        if (admin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        return new ClaimsPrincipal(authenticated ? new ClaimsIdentity(claims, "test") : new ClaimsIdentity());
    }

    private static ActionExecutingContext Context(ClaimsPrincipal user, Dictionary<string, object?> args, string controller = "Test")
    {
        var httpContext = new DefaultHttpContext { User = user };
        var descriptor = new ControllerActionDescriptor { ControllerName = controller };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), args, new object());
    }

    private async Task<(bool nextCalled, ActionExecutingContext context)> Run(ClaimsPrincipal user, Dictionary<string, object?> args, string controller = "Test")
    {
        var context = Context(user, args, controller);
        var called = false;
        await _filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        });
        return (called, context);
    }

    private static void AssertForbidden(ActionExecutingContext context)
    {
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    public class BodyWithShop { public Guid ShopId { get; set; } }

    [Fact]
    public async Task OwnShopId_IsAllowed()
    {
        var (called, _) = await Run(User(), new() { ["shopId"] = _myShop });

        Assert.True(called);
    }

    [Fact]
    public async Task OtherShopId_IsForbidden()
    {
        var (called, context) = await Run(User(), new() { ["shopId"] = _otherShop });

        Assert.False(called);
        AssertForbidden(context);
    }

    [Fact]
    public async Task Admin_CanReachAnyShop()
    {
        var (called, _) = await Run(User(admin: true), new() { ["shopId"] = _otherShop });

        Assert.True(called);
    }

    [Fact]
    public async Task ShopIdInRequestBody_IsChecked()
    {
        var (called, context) = await Run(User(), new() { ["request"] = new BodyWithShop { ShopId = _otherShop } });

        Assert.False(called);
        AssertForbidden(context);
    }

    [Fact]
    public async Task InvoiceOfAnotherShop_IsForbidden()
    {
        var invoiceId = Guid.NewGuid();
        _invoices.Setup(r => r.GetByIdAsync(invoiceId)).ReturnsAsync(new Invoice { Id = invoiceId, ShopId = _otherShop });

        var (called, context) = await Run(User(), new() { ["invoiceId"] = invoiceId });

        Assert.False(called);
        AssertForbidden(context);
    }

    [Fact]
    public async Task OwnSubscription_IsAllowed()
    {
        var id = Guid.NewGuid();
        _subscriptions.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Subscription { Id = id, ShopId = _myShop });

        var (called, _) = await Run(User(), new() { ["subscriptionId"] = id });

        Assert.True(called);
    }

    [Fact]
    public async Task AdOfAnotherShop_IsForbidden()
    {
        var id = Guid.NewGuid();
        _ads.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Advertisement { Id = id, ShopId = _otherShop });

        var (called, _) = await Run(User(), new() { ["adId"] = id });

        Assert.False(called);
    }

    [Fact]
    public async Task WorkflowOfAnotherShop_IsForbidden()
    {
        var id = Guid.NewGuid();
        _workflows.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PublishWorkflow { Id = id, ShopId = _otherShop });

        var (called, _) = await Run(User(), new() { ["workflowId"] = id });

        Assert.False(called);
    }

    [Fact]
    public async Task ApprovalRequestOfAnotherShop_IsForbidden()
    {
        var requestId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        _workflows.Setup(r => r.GetApprovalRequestAsync(requestId))
            .ReturnsAsync(new ApprovalRequest { Id = requestId, PublishWorkflowId = workflowId });
        _workflows.Setup(r => r.GetByIdAsync(workflowId)).ReturnsAsync(new PublishWorkflow { Id = workflowId, ShopId = _otherShop });

        var (called, _) = await Run(User(), new() { ["requestId"] = requestId });

        Assert.False(called);
    }

    [Fact]
    public async Task UnknownResource_PassesThroughSoTheActionCanReturnNotFound()
    {
        var (called, _) = await Run(User(), new() { ["invoiceId"] = Guid.NewGuid() });

        Assert.True(called);
    }

    [Fact]
    public async Task ReviewerQueue_OnlyForTheReviewerThemselves()
    {
        var (okCalled, _) = await Run(User(), new() { ["reviewerId"] = _me });
        var (otherCalled, _) = await Run(User(), new() { ["reviewerId"] = Guid.NewGuid() });

        Assert.True(okCalled);
        Assert.False(otherCalled);
    }

    [Fact]
    public async Task BannerId_ResolvesThroughWorkflow_OnlyOnThePublishWorkflowController()
    {
        var bannerId = Guid.NewGuid();
        _workflows.Setup(r => r.GetByBannerIdAsync(bannerId)).ReturnsAsync(new PublishWorkflow { ShopId = _otherShop });

        var (workflowCalled, _) = await Run(User(), new() { ["bannerId"] = bannerId }, "PublishWorkflow");
        var (otherCalled, _) = await Run(User(), new() { ["bannerId"] = bannerId }, "Banners");

        Assert.False(workflowCalled);
        Assert.True(otherCalled);
    }

    [Fact]
    public async Task Anonymous_IsLeftToTheAuthorizeAttribute()
    {
        var (called, _) = await Run(User(authenticated: false), new() { ["shopId"] = _otherShop });

        Assert.True(called);
    }
}
