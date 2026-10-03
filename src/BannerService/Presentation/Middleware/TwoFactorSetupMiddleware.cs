namespace BannerService.Presentation.Middleware;

using Domain.Services;

/// <summary>
/// An administrator who has not yet set up two-step sign-in (where it is required) can only reach the sign-in and account screens that set it up;
/// everything else answers 403 until it is on.
/// </summary>
public class TwoFactorSetupMiddleware
{
    private readonly RequestDelegate _next;

    public TwoFactorSetupMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var pending = context.User.Identity?.IsAuthenticated == true && context.User.HasClaim(JwtTokenService.TwoFactorSetupClaim, "true");

        if (pending && path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/api/account", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/api/authentication", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/api/notifications", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"message\":\"Administrators must set up two-step sign-in first. Open My account.\",\"code\":\"two_factor_setup_required\"}");
            return;
        }

        await _next(context);
    }
}
