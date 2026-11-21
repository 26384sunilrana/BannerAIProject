using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Presentation.Middleware;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>The application with the limits switched on and set low.</summary>
public class LimitedFactory : SmokeFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:Enabled", "true");
        builder.UseSetting("RateLimiting:AuthPerMinute", "4");
        builder.UseSetting("RateLimiting:ApiPerMinute", "6");
    }
}

public class RateLimitSmokeTests : IClassFixture<LimitedFactory>
{
    private readonly LimitedFactory _factory;

    public RateLimitSmokeTests(LimitedFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GuessingPasswords_IsSlowedDown_WithAClearAnswer_AndOtherCallersAreNotAffected()
    {
        var client = _factory.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
            codes.Add((await client.PostAsJsonAsync("/api/authentication/login", new { email = "nobody@example.com", password = "wrong-password" })).StatusCode);

        Assert.Equal(4, codes.Count(c => c != HttpStatusCode.TooManyRequests));
        Assert.Equal(2, codes.Count(c => c == HttpStatusCode.TooManyRequests));

        var refused = await client.PostAsJsonAsync("/api/authentication/login", new { email = "nobody@example.com", password = "x" });
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.True(int.Parse(refused.Headers.GetValues("Retry-After").Single()) > 0);
        var body = await refused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rate_limited", body.GetProperty("code").GetString());
        Assert.Contains("wait a minute", body.GetProperty("message").GetString());

        // sign-up shares the sign-in limit, and the refusal is in the activity log of attempts
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/authentication/register", new { email = "x@example.com", password = "Password123!", firstName = "A", lastName = "B", shopName = "S" })).StatusCode);

        // the rest of the API has its own, wider limit
        var api = new List<HttpStatusCode>();
        for (var i = 0; i < 8; i++) api.Add((await client.GetAsync("/api/locations/countries")).StatusCode);
        Assert.Equal(6, api.Count(c => c != HttpStatusCode.TooManyRequests));
        Assert.Equal(2, api.Count(c => c == HttpStatusCode.TooManyRequests));
    }

    [Fact]
    public async Task HealthChecksAreNeverLimited()
    {
        var client = _factory.CreateClient();
        for (var i = 0; i < 15; i++)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.GetAsync("/health")).StatusCode);
    }

    [Theory]
    [InlineData("/health", "GET", RateClass.None)]
    [InlineData("/api/authentication/login", "POST", RateClass.Authentication)]
    [InlineData("/api/authentication/register", "POST", RateClass.Authentication)]
    [InlineData("/api/authentication/refresh", "POST", RateClass.Refresh)]
    [InlineData("/api/authentication/me", "GET", RateClass.Api)]
    [InlineData("/api/authentication/ME/", "GET", RateClass.Api)]
    [InlineData("/api/authentication/me", "POST", RateClass.Authentication)]
    [InlineData("/api/authentication/menu", "GET", RateClass.Authentication)]
    [InlineData("/api/authentication/logout", "POST", RateClass.Authentication)]
    [InlineData("/api/screens/pairing/poll", "POST", RateClass.Pairing)]
    [InlineData("/api/screens/token", "POST", RateClass.Pairing)]
    [InlineData("/api/media/upload/initialize", "POST", RateClass.Upload)]
    [InlineData("/api/media/3f2c/chunks/4", "PUT", RateClass.Upload)]
    [InlineData("/api/media/3f2c/chunks/4", "GET", RateClass.Api)]
    [InlineData("/api/banners", "GET", RateClass.Api)]
    [InlineData(null, null, RateClass.Api)]
    public void RequestsAreSortedIntoTheRightClass(string? path, string? method, RateClass expected) =>
        Assert.Equal(expected, RateLimitRules.Classify(path, method));
}

/// <summary>"Who am I" is asked on every page the person opens; it must not use up the strict sign-in limit.</summary>
public class WhoAmIRateLimitSmokeTests : IClassFixture<LimitedFactory>
{
    private readonly LimitedFactory _factory;

    public WhoAmIRateLimitSmokeTests(LimitedFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AskingWhoAmI_DoesNotUseUpTheSignInLimit_AndSignInIsStillLimited()
    {
        var client = _factory.CreateClient();

        // sign-in is limited to 4 a minute in this factory: five calls to /me would use it up if they counted there
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/authentication/me")).StatusCode);

        var logins = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            logins.Add((await client.PostAsJsonAsync("/api/authentication/login", new { email = "nobody@example.com", password = "wrong-password" })).StatusCode);

        Assert.Equal(4, logins.Count(c => c != HttpStatusCode.TooManyRequests));
        Assert.Equal(HttpStatusCode.TooManyRequests, logins[4]);
    }
}
