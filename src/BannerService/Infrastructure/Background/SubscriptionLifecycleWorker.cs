namespace BannerService.Infrastructure.Background;

using Application.Services;

/// <summary>
/// Runs the subscription lifecycle (renewals, reminders, grace and expiry) on a timer.
/// Subscriptions:LifecycleIntervalMinutes sets how often (default 60); Subscriptions:LifecycleEnabled=false turns it off.
/// Several servers may run it at once: every step is repeatable and messages are claimed in the database.
/// </summary>
public class SubscriptionLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionLifecycleWorker> _logger;

    public SubscriptionLifecycleWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<SubscriptionLifecycleWorker> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Subscriptions:LifecycleEnabled", true))
        {
            _logger.LogInformation("Subscription lifecycle job is switched off");
            return;
        }

        var minutes = Math.Max(1, _configuration.GetValue("Subscriptions:LifecycleIntervalMinutes", 60));

        // Let start-up (migrations) finish before the first run
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var report = await scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleService>().RunAsync(DateTime.UtcNow);
                _logger.LogInformation(
                    "Subscription lifecycle: {Renewed} renewed, {Failed} payments failed, {Grace} entered grace, {Expired} expired, {Messages} messages sent",
                    report.AutoRenewed, report.RenewalsFailed, report.EnteredGrace, report.Expired, report.MessagesSent);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Subscription lifecycle run failed");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); } catch (OperationCanceledException) { return false; }
    }
}
