namespace BannerService.Presentation.Middleware;

using Infrastructure.Data;
using System.Security.Claims;

public class ShopContextMiddleware
{
    private readonly RequestDelegate _next;

    public ShopContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IShopContextAccessor shopContextAccessor)
    {
        try
        {
            // Anonymous requests (login, register, health) have no shop; [Authorize] rejects them where needed.
            // Authenticated requests only get a shop context when the token carries a valid shop_id.
            var shopIdClaim = context.User.FindFirst("shop_id")?.Value;

            if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(shopIdClaim, out var shopId))
                shopContextAccessor.SetShopId(shopId);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An unexpected error occurred"
                }
            });
            return;
        }

        await _next(context);
    }
}
