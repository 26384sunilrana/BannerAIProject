namespace BannerService.Infrastructure.Monitoring;

using System.Collections.Concurrent;
using Application.Services;
using Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

/// <summary>
/// When each background job last finished well. A job that stops (a crash loop, a stuck database) shows up here, in the readiness check, in the metrics
/// and as a message to the administrators. Kept in memory: each API pod reports for itself.
/// </summary>
public static class JobHeartbeats
{
    public sealed record JobState(TimeSpan Interval, DateTime StartedAt, DateTime? LastSuccess, DateTime? LastFailure, string? LastError);

    private static readonly ConcurrentDictionary<string, JobState> Jobs = new();

    public static void Expect(string name, TimeSpan interval) =>
        Jobs[name] = new JobState(interval, DateTime.UtcNow, null, null, null);

    public static void Succeeded(string name) =>
        Jobs.AddOrUpdate(name, _ => new JobState(TimeSpan.FromHours(1), DateTime.UtcNow, DateTime.UtcNow, null, null),
            (_, old) => old with { LastSuccess = DateTime.UtcNow });

    public static void Failed(string name, string error) =>
        Jobs.AddOrUpdate(name, _ => new JobState(TimeSpan.FromHours(1), DateTime.UtcNow, null, DateTime.UtcNow, error),
            (_, old) => old with { LastFailure = DateTime.UtcNow, LastError = error });

    public static IReadOnlyDictionary<string, JobState> Snapshot() => new Dictionary<string, JobState>(Jobs);

    public static void Clear() => Jobs.Clear();

    /// <summary>A job is late when it has not succeeded within three of its intervals (and two minutes of grace) since it was last seen alive.</summary>
    public static bool IsLate(JobState job, DateTime now)
    {
        var reference = job.LastSuccess ?? job.StartedAt;
        return now - reference > job.Interval * 3 + TimeSpan.FromMinutes(2);
    }
}

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("The database answers.")
                : HealthCheckResult.Unhealthy("The database cannot be reached.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("The database check failed.", ex);
        }
    }
}

/// <summary>Writes and removes a small file where uploaded media is kept: a full or read-only disk is found before a customer finds it.</summary>
public class StorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public StorageHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var folder = _configuration["MediaService:LocalStoragePath"];
        if (string.IsNullOrWhiteSpace(folder)) return Task.FromResult(HealthCheckResult.Healthy("No local media folder is configured."));

        try
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return Task.FromResult(HealthCheckResult.Healthy("The media folder can be written."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("The media folder cannot be written.", ex));
        }
    }
}

/// <summary>Degraded (not failed: the API still serves) when a background job is late.</summary>
public class JobsHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var late = JobHeartbeats.Snapshot().Where(j => JobHeartbeats.IsLate(j.Value, DateTime.UtcNow)).Select(j => j.Key).ToList();
        return Task.FromResult(late.Count == 0
            ? HealthCheckResult.Healthy("Background jobs are on time.")
            : HealthCheckResult.Degraded("Late jobs: " + string.Join(", ", late)));
    }
}

/// <summary>Numbers for the metrics page that are read from the database each time it is scraped.</summary>
public static class BusinessMetrics
{
    public static readonly Counter RateLimited = Metrics.CreateCounter("bannerai_rate_limited_total", "Requests refused by the rate limits.", "kind");
    private static readonly Gauge ShopsActive = Metrics.CreateGauge("bannerai_shops_active", "Active shops.");
    private static readonly Gauge AdsPending = Metrics.CreateGauge("bannerai_ads_waiting", "Ads waiting for someone to decide.", "kind");
    private static readonly Gauge UsersLocked = Metrics.CreateGauge("bannerai_users_locked_out", "Logins locked after too many wrong passwords or codes.");
    private static readonly Gauge ScreensOnline = Metrics.CreateGauge("bannerai_screens_online", "Paired screens heard from in the last three minutes.");
    private static readonly Gauge ScreensOffline = Metrics.CreateGauge("bannerai_screens_offline", "Paired screens not heard from for three minutes.");
    private static readonly Gauge FailedSignIns = Metrics.CreateGauge("bannerai_failed_sign_ins_5m", "Refused sign-in attempts in the last five minutes.");
    private static readonly Gauge JobSuccess = Metrics.CreateGauge("bannerai_job_last_success_timestamp_seconds", "When a background job last finished well.", "job");
    private static readonly Gauge JobLate = Metrics.CreateGauge("bannerai_job_late", "1 when a background job is late.", "job");

    public static void Register(IServiceScopeFactory scopes)
    {
        Metrics.DefaultRegistry.AddBeforeCollectCallback(async cancellation =>
        {
            foreach (var (name, job) in JobHeartbeats.Snapshot())
            {
                JobSuccess.WithLabels(name).Set(job.LastSuccess.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(job.LastSuccess.Value, DateTimeKind.Utc)).ToUnixTimeSeconds() : 0);
                JobLate.WithLabels(name).Set(JobHeartbeats.IsLate(job, DateTime.UtcNow) ? 1 : 0);
            }

            try
            {
                using var scope = scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                ShopsActive.Set(await context.Shops.CountAsync(s => s.Status == ShopStatus.Active, cancellation));
                AdsPending.WithLabels("approval").Set(await context.ShopAds.CountAsync(a => a.Status == ShopAdStatus.PendingApproval, cancellation));
                AdsPending.WithLabels("compliance").Set(await context.ShopAds.CountAsync(a => a.Status == ShopAdStatus.PendingCompliance, cancellation));
                UsersLocked.Set(await context.Users.CountAsync(u => u.IsLockedOut, cancellation));
                FailedSignIns.Set(await Watchdog.CountFailedSignInsAsync(context, DateTime.UtcNow, cancellation));
                var window = DateTime.UtcNow - ScreenService.OnlineWindow;
                var online = await context.Screens.CountAsync(s => s.Status == ScreenStatus.Active && s.LastSeenAt != null && s.LastSeenAt >= window, cancellation);
                ScreensOnline.Set(online);
                ScreensOffline.Set(await context.Screens.CountAsync(s => s.Status == ScreenStatus.Active, cancellation) - online);
            }
            catch
            {
                // the page must still answer when the database does not: the readiness check reports that
            }
        });
    }
}

public static class WatchdogRules
{
    /// <summary>True when there were enough refused sign-ins in the window to be worth waking someone for.</summary>
    public static bool IsSpike(int failures, int threshold) => threshold > 0 && failures >= threshold;

    /// <summary>An alert of one kind is sent at most once in this long.</summary>
    public static bool MaySend(DateTime? lastSent, DateTime now, TimeSpan quiet) => lastSent == null || now - lastSent.Value >= quiet;
}

public static class Watchdog
{
    public static Task<int> CountFailedSignInsAsync(ApplicationDbContext context, DateTime now, CancellationToken token) =>
        context.AuditLogs.CountAsync(a => a.OccurredAt >= now.AddMinutes(-5) && a.Path.StartsWith("/api/authentication/login")
                                          && (a.StatusCode == 401 || a.StatusCode == 429), token);
}

/// <summary>
/// Looks for trouble every minute and tells the administrators in the application (the bell): a spike of refused sign-ins (someone guessing passwords)
/// and background jobs that stopped. Each kind of alert is sent at most once an hour. Monitoring:WatchdogEnabled=false switches it off;
/// Monitoring:FailedSignInsPer5Minutes (default 30) sets the spike.
/// </summary>
public class OpsWatchdog : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpsWatchdog> _logger;
    private readonly Dictionary<string, DateTime> _lastSent = new();

    public OpsWatchdog(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<OpsWatchdog> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Monitoring:WatchdogEnabled", true)) return;

        try { await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try { await CheckOnceAsync(DateTime.UtcNow, stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogError(ex, "The watchdog check failed"); }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    public async Task CheckOnceAsync(DateTime now, CancellationToken token)
    {
        using var scope = _scopes.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var failures = await Watchdog.CountFailedSignInsAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), now, token);
        if (WatchdogRules.IsSpike(failures, _configuration.GetValue("Monitoring:FailedSignInsPer5Minutes", 30)) && MayAlert("signins", now))
            await notifications.NotifyAdminsAsync("SecurityAlert", "Many failed sign-ins",
                $"{failures} sign-in attempts were refused in the last five minutes. Someone may be guessing passwords. See Activity, failures only.", "/admin/audit-log");

        // a screen that went quiet: its owner is told once per outage (not again for six hours)
        var offlineAfter = TimeSpan.FromMinutes(Math.Max(3, _configuration.GetValue("Monitoring:ScreenOfflineMinutes", 10)));
        var screens = scope.ServiceProvider.GetRequiredService<Domain.Interfaces.IScreenRepository>();
        foreach (var screen in await screens.ListActiveAsync())
        {
            var reference = screen.LastSeenAt ?? screen.CreatedAt;
            if (now - reference < offlineAfter) { screen.OfflineNoticeAt = null; continue; }
            if (!WatchdogRules.MaySend(screen.OfflineNoticeAt, now, TimeSpan.FromHours(6))) continue;

            screen.OfflineNoticeAt = now;
            await screens.SaveAsync();
            await notifications.NotifyShopOwnersAsync(screen.ShopId, "ScreenOffline", "Your shop screen is offline",
                $"\"{screen.Name}\" has not been heard from since {reference:d MMM HH:mm} (UTC). Check that it is switched on and online. It shows the default board meanwhile.", "/screens");
        }
        await screens.SaveAsync();

        foreach (var (name, job) in JobHeartbeats.Snapshot().Where(j => JobHeartbeats.IsLate(j.Value, now)))
            if (MayAlert("job:" + name, now))
                await notifications.NotifyAdminsAsync("JobLate", "A background job has stopped",
                    $"The job \"{name}\" has not finished well for a long time" + (job.LastError == null ? "." : $" (last error: {job.LastError})."), "/admin/audit-log");
    }

    private bool MayAlert(string key, DateTime now)
    {
        _lastSent.TryGetValue(key, out var last);
        if (!WatchdogRules.MaySend(_lastSent.ContainsKey(key) ? last : null, now, TimeSpan.FromHours(1))) return false;
        _lastSent[key] = now;
        return true;
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); } catch (OperationCanceledException) { return false; }
    }
}
