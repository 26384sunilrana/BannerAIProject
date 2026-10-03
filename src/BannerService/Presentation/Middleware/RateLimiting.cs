namespace BannerService.Presentation.Middleware;

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>Which kind of request this is, for the limits.</summary>
public enum RateClass
{
    /// <summary>Not limited: health checks.</summary>
    None,

    /// <summary>Sign-in, sign-up and the other ways of proving who you are. Strictest, by IP address.</summary>
    Authentication,

    /// <summary>Session refresh. By IP address.</summary>
    Refresh,

    /// <summary>A screen asking for a pairing code or waiting for it (every few seconds). By IP address.</summary>
    Pairing,

    /// <summary>Pieces of uploaded files. Many requests in a minute are normal for a big video.</summary>
    Upload,

    /// <summary>Everything else.</summary>
    Api,
}

public static class RateLimitRules
{
    public static RateClass Classify(string? path, string? method)
    {
        path ??= string.Empty;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)) return RateClass.None;
        if (path.StartsWith("/api/screens/pairing", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/screens/token", StringComparison.OrdinalIgnoreCase)) return RateClass.Pairing;
        if (path.StartsWith("/api/authentication/refresh", StringComparison.OrdinalIgnoreCase)) return RateClass.Refresh;
        if (path.StartsWith("/api/authentication", StringComparison.OrdinalIgnoreCase)) return RateClass.Authentication;
        if (path.StartsWith("/api/media/upload", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase) && path.Contains("/chunks/", StringComparison.OrdinalIgnoreCase)))
            return RateClass.Upload;
        return RateClass.Api;
    }
}

/// <summary>
/// Limits how fast one caller can hit the API, so that guessing passwords, flooding sign-up or hammering uploads is slowed down.
/// Sign-in and sign-up are limited by IP address (strictly); everything else by signed-in user, or by IP address when nobody is signed in.
/// Settings (RateLimiting:*): Enabled, AuthPerMinute (20), RefreshPerMinute (60), UploadPerMinute (600), ApiPerMinute (1200).
/// </summary>
public static class RateLimiting
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("RateLimiting:Enabled", true);
        var auth = Math.Max(1, configuration.GetValue("RateLimiting:AuthPerMinute", 20));
        var refresh = Math.Max(1, configuration.GetValue("RateLimiting:RefreshPerMinute", 60));
        var upload = Math.Max(1, configuration.GetValue("RateLimiting:UploadPerMinute", 600));
        var pairing = Math.Max(1, configuration.GetValue("RateLimiting:PairingPerMinute", 120));
        var api = Math.Max(1, configuration.GetValue("RateLimiting:ApiPerMinute", 1200));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                Infrastructure.Monitoring.BusinessMetrics.RateLimited.WithLabels(RateLimitRules.Classify(context.HttpContext.Request.Path.Value, context.HttpContext.Request.Method).ToString().ToLowerInvariant()).Inc();
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString();

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    "{\"message\":\"Too many requests. Please wait a minute and try again.\",\"code\":\"rate_limited\"}", token);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!enabled) return RateLimitPartition.GetNoLimiter("off");

                var kind = RateLimitRules.Classify(context.Request.Path.Value, context.Request.Method);
                if (kind == RateClass.None) return RateLimitPartition.GetNoLimiter("none");

                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var user = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var (key, limit) = kind switch
                {
                    RateClass.Authentication => ($"auth:{ip}", auth),
                    RateClass.Refresh => ($"refresh:{ip}", refresh),
                    RateClass.Pairing => ($"pairing:{ip}", pairing),
                    RateClass.Upload => ($"upload:{user ?? ip}", upload),
                    _ => ($"api:{user ?? ip}", api),
                };

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                });
            });
        });

        return services;
    }
}
