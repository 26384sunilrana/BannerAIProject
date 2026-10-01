namespace BannerService.Presentation.Middleware;

using System.Diagnostics;
using System.Security.Claims;
using Domain.Entities;
using Domain.Interfaces;

/// <summary>
/// Records every API call by a signed-in user (reads included, for HIPAA-style access tracking)
/// and every call to the authentication endpoints. Never stores bodies.
/// </summary>
public class AuditLoggingMiddleware
{
    private static readonly string[] SkippedPrefixes = { "/health", "/swagger", "/favicon" };
    private const string AuthPrefix = "/api/authentication";

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (SkippedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var watch = Stopwatch.StartNew();
        var failed = false;
        try
        {
            await _next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            watch.Stop();
            await WriteAsync(context, path, failed, (int)watch.ElapsedMilliseconds);
        }
    }

    private async Task WriteAsync(HttpContext context, string path, bool failed, int elapsedMs)
    {
        try
        {
            // The authentication middleware runs inside this one, so the user is known by now
            var user = context.User;
            var signedIn = user.Identity?.IsAuthenticated == true;
            if (!signedIn && !path.StartsWith(AuthPrefix, StringComparison.OrdinalIgnoreCase))
                return;

            Guid? shopId = Guid.TryParse(user.FindFirst("shop_id")?.Value, out var shop) ? shop : null;

            // A separate scope (and DbContext) so a failed request's half-applied changes are never saved with the log entry
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            await repository.AddAsync(new AuditLog
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserEmail = user.FindFirst(ClaimTypes.Email)?.Value,
                ShopId = shopId,
                Method = context.Request.Method,
                Path = path.Length > 512 ? path[..512] : path,
                StatusCode = failed ? StatusCodes.Status500InternalServerError : context.Response.StatusCode,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                DurationMs = elapsedMs
            });
        }
        catch (Exception ex)
        {
            // Auditing must never take the API down
            _logger.LogError(ex, "Failed to write audit log entry");
        }
    }
}
