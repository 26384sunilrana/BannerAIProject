namespace BannerService.Presentation.Middleware;

using System.Text.RegularExpressions;

/// <summary>
/// A screen's token is for reading what a screen shows and nothing else. Whatever other endpoint allows "any signed-in caller", a screen is
/// turned away here: it may only read the active banner, that banner and its media links, its shop's default board and live ads, and send its heartbeat.
/// </summary>
public class ScreenRestrictionMiddleware
{
    private static readonly Regex[] Allowed =
    {
        new(@"^/api/banners/active/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^/api/banners/[0-9a-f-]{36}/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^/api/banners/[0-9a-f-]{36}/preview/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^/api/media/[0-9a-f-]{36}/url/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^/api/shops/[0-9a-f-]{36}/default-board/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^/api/shops/[0-9a-f-]{36}/ads/live/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    private readonly RequestDelegate _next;

    public ScreenRestrictionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Screen"))
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var read = HttpMethods.IsGet(context.Request.Method) && Allowed.Any(r => r.IsMatch(path));
            var heartbeat = HttpMethods.IsPost(context.Request.Method) && path.Equals("/api/screens/heartbeat", StringComparison.OrdinalIgnoreCase);

            if (!read && !heartbeat)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"A screen may only read what it shows.\",\"code\":\"screen_restricted\"}");
                return;
            }
        }

        await _next(context);
    }
}
