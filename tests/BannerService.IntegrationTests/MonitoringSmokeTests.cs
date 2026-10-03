using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Monitoring;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>The application with the metrics page behind a token.</summary>
public class MetricsTokenFactory : SmokeFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Metrics:Token", "scrape-secret-123");
    }
}

public class MonitoringSmokeTests : IClassFixture<SmokeFactory>, IClassFixture<MetricsTokenFactory>
{
    private readonly SmokeFactory _factory;
    private readonly MetricsTokenFactory _guarded;

    public MonitoringSmokeTests(SmokeFactory factory, MetricsTokenFactory guarded)
    {
        _factory = factory;
        _guarded = guarded;
    }

    [Fact]
    public async Task Live_AnswersAlways_AndReady_ChecksTheDatabaseTheMediaFolderAndTheJobs()
    {
        var client = _factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("healthy", (await live.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var body = await ready.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("healthy", body.GetProperty("status").GetString());
        var checks = body.GetProperty("checks");
        Assert.Equal("healthy", checks.GetProperty("database").GetProperty("status").GetString());
        Assert.Equal("healthy", checks.GetProperty("storage").GetProperty("status").GetString());
        Assert.True(checks.TryGetProperty("jobs", out _));

        // the older address still answers for anything that uses it
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task TheMetricsPage_ShowsRequestsAndTheNumbersOfTheBusiness()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        await client.GetAsync("/api/locations/countries");

        var response = await client.GetAsync("/metrics");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http_requests_received_total", text);
        Assert.Contains("bannerai_shops_active", text);
        Assert.Contains("bannerai_ads_waiting{kind=\"compliance\"}", text);
        Assert.Contains("bannerai_users_locked_out", text);
        Assert.Contains("bannerai_failed_sign_ins_5m", text);
    }

    [Fact]
    public async Task WithAToken_TheMetricsPageIsClosedWithoutIt()
    {
        var client = _guarded.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/metrics")).StatusCode);

        var wrong = new HttpRequestMessage(HttpMethod.Get, "/metrics");
        wrong.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-the-secret");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(wrong)).StatusCode);

        var right = new HttpRequestMessage(HttpMethod.Get, "/metrics");
        right.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "scrape-secret-123");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(right)).StatusCode);
    }

    private async Task<(OpsWatchdog dog, ApplicationDbContext context, Func<Task<List<string>>> adminKinds)> WatchdogAsync()
    {
        await _factory.SeedAsync();
        var scopes = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Monitoring:FailedSignInsPer5Minutes"] = "30" }).Build();
        var dog = new OpsWatchdog(scopes, config, NullLogger<OpsWatchdog>.Instance);
        var scope = scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        async Task<List<string>> AdminKinds()
        {
            using var inner = scopes.CreateScope();
            var db = inner.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var adminId = (await db.Users.FirstAsync(u => u.Email == "admin@example.com")).Id;
            return await db.InAppNotifications.Where(n => n.UserId == adminId).Select(n => n.Kind).ToListAsync();
        }
        return (dog, context, AdminKinds);
    }

    [Fact]
    public async Task ASpikeOfRefusedSignIns_IsToldToAdministrators_OnceAnHour()
    {
        var (dog, context, adminKinds) = await WatchdogAsync();
        var before = (await adminKinds()).Count(k => k == "SecurityAlert");
        var now = DateTime.UtcNow;

        for (var i = 0; i < 29; i++)
            context.AuditLogs.Add(new AuditLog { OccurredAt = now.AddMinutes(-1), Method = "POST", Path = "/api/authentication/login", StatusCode = 401 });
        await context.SaveChangesAsync();
        await dog.CheckOnceAsync(now, default);
        Assert.Equal(before, (await adminKinds()).Count(k => k == "SecurityAlert")); // 29 is below the line

        context.AuditLogs.Add(new AuditLog { OccurredAt = now.AddMinutes(-1), Method = "POST", Path = "/api/authentication/login", StatusCode = 429 });
        await context.SaveChangesAsync();
        await dog.CheckOnceAsync(now, default);
        Assert.Equal(before + 1, (await adminKinds()).Count(k => k == "SecurityAlert"));

        await dog.CheckOnceAsync(now.AddMinutes(10), default); // still a spike, but told already
        Assert.Equal(before + 1, (await adminKinds()).Count(k => k == "SecurityAlert"));
    }

    [Fact]
    public async Task AJobThatStopped_IsToldToAdministrators_AndNeverMakesTheApiUnready()
    {
        var (dog, _, adminKinds) = await WatchdogAsync();
        var before = (await adminKinds()).Count(k => k == "JobLate");
        JobHeartbeats.Expect("test-job-" + Guid.NewGuid().ToString("N")[..6], TimeSpan.FromMinutes(1));

        await dog.CheckOnceAsync(DateTime.UtcNow.AddHours(2), default);
        Assert.True((await adminKinds()).Count(k => k == "JobLate") > before);

        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public void ALateJobIsOneThatMissedThreeRoundsAndTwoMinutes()
    {
        var started = new DateTime(2030, 1, 7, 10, 0, 0, DateTimeKind.Utc);
        var job = new JobHeartbeats.JobState(TimeSpan.FromMinutes(10), started, null, null, null);

        Assert.False(JobHeartbeats.IsLate(job, started.AddMinutes(31)));
        Assert.True(JobHeartbeats.IsLate(job, started.AddMinutes(33)));
        Assert.False(JobHeartbeats.IsLate(job with { LastSuccess = started.AddMinutes(30) }, started.AddMinutes(60)));
        Assert.True(JobHeartbeats.IsLate(job with { LastSuccess = started.AddMinutes(30) }, started.AddMinutes(63)));
    }

    [Theory]
    [InlineData(29, 30, false)]
    [InlineData(30, 30, true)]
    [InlineData(500, 30, true)]
    [InlineData(500, 0, false)]
    public void ASpikeIsAtLeastTheThreshold_AndZeroSwitchesItOff(int failures, int threshold, bool expected) =>
        Assert.Equal(expected, WatchdogRules.IsSpike(failures, threshold));

    [Fact]
    public void AnAlertIsSentOnceInTheQuietTime()
    {
        var now = new DateTime(2030, 1, 7, 10, 0, 0, DateTimeKind.Utc);

        Assert.True(WatchdogRules.MaySend(null, now, TimeSpan.FromHours(1)));
        Assert.False(WatchdogRules.MaySend(now.AddMinutes(-59), now, TimeSpan.FromHours(1)));
        Assert.True(WatchdogRules.MaySend(now.AddMinutes(-60), now, TimeSpan.FromHours(1)));
    }
}
