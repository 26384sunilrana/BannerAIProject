namespace BannerService.Presentation.Security;

using System.Security.Claims;
using BannerService.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

public static class TenantAccess
{
    public const string AdminRole = "Admin";

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(AdminRole);

    /// <summary>Admins can reach every shop; everyone else only the shop in their token.</summary>
    public static bool CanAccessShop(this ClaimsPrincipal user, Guid shopId)
    {
        if (user.IsAdmin())
            return true;

        return Guid.TryParse(user.FindFirst("shop_id")?.Value, out var own) && own == shopId;
    }
}

/// <summary>
/// Stops authenticated users from reaching another shop's data. Checks every action argument named
/// shopId, any request body with a ShopId, and resource ids (ad, report, invoice, subscription,
/// workflow, approval request) by looking up the shop that owns them.
/// </summary>
public class TenantAccessFilter : IAsyncActionFilter
{
    private readonly IAdvertisementRepository _ads;
    private readonly IAnalyticsRepository _analytics;
    private readonly IInvoiceRepository _invoices;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPublishWorkflowRepository _workflows;

    public TenantAccessFilter(
        IAdvertisementRepository ads,
        IAnalyticsRepository analytics,
        IInvoiceRepository invoices,
        ISubscriptionRepository subscriptions,
        IPublishWorkflowRepository workflows)
    {
        _ads = ads;
        _analytics = analytics;
        _invoices = invoices;
        _subscriptions = subscriptions;
        _workflows = workflows;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true || user.IsAdmin())
        {
            await next();
            return;
        }

        foreach (var shopId in ShopIdsInArguments(context))
        {
            if (!user.CanAccessShop(shopId))
            {
                Deny(context);
                return;
            }
        }

        foreach (var (name, id) in GuidArguments(context))
        {
            var owner = await ResolveOwningShopAsync(context, name, id, user);
            if (owner.denied || (owner.shopId.HasValue && !user.CanAccessShop(owner.shopId.Value)))
            {
                Deny(context);
                return;
            }
        }

        await next();
    }

    private static void Deny(ActionExecutingContext context) =>
        context.Result = new ObjectResult(new { success = false, message = "You do not have access to this shop's data" })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

    private static IEnumerable<Guid> ShopIdsInArguments(ActionExecutingContext context)
    {
        foreach (var (name, value) in context.ActionArguments)
        {
            if (value is Guid direct && string.Equals(name, "shopId", StringComparison.OrdinalIgnoreCase))
            {
                yield return direct;
            }
            else if (value != null && !value.GetType().IsPrimitive && value is not string)
            {
                var property = value.GetType().GetProperty("ShopId");
                if (property?.PropertyType == typeof(Guid) && property.GetValue(value) is Guid bodyShop && bodyShop != Guid.Empty)
                    yield return bodyShop;
            }
        }
    }

    private static IEnumerable<(string name, Guid id)> GuidArguments(ActionExecutingContext context) =>
        context.ActionArguments
            .Where(a => a.Value is Guid && !string.Equals(a.Key, "shopId", StringComparison.OrdinalIgnoreCase))
            .Select(a => (a.Key, (Guid)a.Value!));

    private async Task<(Guid? shopId, bool denied)> ResolveOwningShopAsync(
        ActionExecutingContext context, string name, Guid id, ClaimsPrincipal user)
    {
        switch (name.ToLowerInvariant())
        {
            case "adid":
                return ((await _ads.GetByIdAsync(id))?.ShopId, false);
            case "reportid":
                return ((await _analytics.GetReportByIdAsync(id))?.ShopId, false);
            case "invoiceid":
                return ((await _invoices.GetByIdAsync(id))?.ShopId, false);
            case "subscriptionid":
                return ((await _subscriptions.GetByIdAsync(id))?.ShopId, false);
            case "workflowid":
                return ((await _workflows.GetByIdAsync(id))?.ShopId, false);
            case "requestid":
                var request = await _workflows.GetApprovalRequestAsync(id);
                return (request == null ? null : (await _workflows.GetByIdAsync(request.PublishWorkflowId))?.ShopId, false);
            case "reviewerid":
                // Reviewers may only see their own queue
                return (null, !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var self) || self != id);
            case "bannerid" when context.ActionDescriptor is ControllerActionDescriptor { ControllerName: "PublishWorkflow" }:
                return ((await _workflows.GetByBannerIdAsync(id))?.ShopId, false);
            default:
                return (null, false);
        }
    }
}
