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
            var shopIdClaim = context.User.FindFirst("shop_id")?.Value;

            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out var shopId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = "UNAUTHORIZED",
                        message = "Missing or invalid shop_id in token"
                    }
                });
                return;
            }

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
