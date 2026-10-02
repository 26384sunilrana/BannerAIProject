namespace BannerService.Infrastructure.Background;

using Application.Services;

/// <summary>
/// Tidies the media store on a timer: Media:Cleanup:Enabled=false turns it off, Media:Cleanup:IntervalHours sets how often (default 6).
/// </summary>
public class MediaCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaCleanupWorker> _logger;

    public MediaCleanupWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<MediaCleanupWorker> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Media:Cleanup:Enabled", true))
        {
            _logger.LogInformation("Media clean-up is switched off");
            return;
        }

        var hours = Math.Max(1, _configuration.GetValue("Media:Cleanup:IntervalHours", 6));

        try { await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); } catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<MediaCleanupService>().RunAsync(DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Media clean-up failed");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); } catch (OperationCanceledException) { return false; }
    }
}
