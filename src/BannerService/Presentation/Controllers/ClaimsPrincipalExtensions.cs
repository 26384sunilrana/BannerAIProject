namespace BannerService.Presentation.Controllers;

using System.Security.Claims;

/// <summary>Reads the caller's identity from the validated JWT instead of trusting request headers.</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user id in token");
        return userId;
    }

    public static string GetUserName(this ClaimsPrincipal user) =>
        user.Identity?.Name
        ?? user.FindFirst(ClaimTypes.Name)?.Value
        ?? user.FindFirst(ClaimTypes.Email)?.Value
        ?? "user";

    public static Guid GetShopId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("shop_id")?.Value;
        if (!Guid.TryParse(value, out var shopId))
            throw new UnauthorizedAccessException("Invalid or missing shop_id in token");
        return shopId;
    }
}
