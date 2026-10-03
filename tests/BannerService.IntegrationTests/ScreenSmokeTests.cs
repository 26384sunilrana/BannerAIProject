using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>Pairing a screen with a code, what its token may read, proof of play and the offline notice, through the real application.</summary>
public class ScreenSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public ScreenSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) throw new Exception($"{(int)response.StatusCode} {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<(HttpClient owner, Guid shopId, string email)> OwnerAsync()
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var email = $"screen-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Sol", lastName = "Screen", shopName = "Screen Shop " + email });
        var token = (await Json(register)).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value), email);
    }

    private async Task<(string code, string secret)> TvStartsAsync()
    {
        var start = await Json(await _factory.CreateClient().PostAsync("/api/screens/pairing/start", null));
        return (start.GetProperty("code").GetString()!, start.GetProperty("deviceSecret").GetString()!);
    }

    private static HttpRequestMessage TokenRequest(string secret)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/screens/token");
        request.Headers.Add("X-Screen-Token", secret);
        return request;
    }

    private async Task<HttpClient> ScreenClientAsync(string secret)
    {
        var tv = _factory.CreateClient();
        var token = await Json(await tv.SendAsync(TokenRequest(secret)));
        tv.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString()!);
        return tv;
    }

    [Fact]
    public async Task APairedScreen_GetsATokenThatReadsOnlyWhatAScreenShows()
    {
        var (owner, shopId, _) = await OwnerAsync();
        var (otherOwner, otherShop, _) = await OwnerAsync();

        // the television shows a code and waits
        var (code, secret) = await TvStartsAsync();
        Assert.Matches("^[A-HJ-NP-Z2-9]{6}$", code);
        var waiting = await Json(await _factory.CreateClient().PostAsJsonAsync("/api/screens/pairing/poll", new { deviceSecret = secret }));
        Assert.Equal("waiting", waiting.GetProperty("status").GetString());

        // a wrong code, then the right one typed in lower case with a dash; an executive cannot do it
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code = "ZZZZZZ", name = "Shop TV" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code = "AB", name = "Shop TV" })).StatusCode);
        var paired = await Json(await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code = code[..3].ToLowerInvariant() + "-" + code[3..], name = "Shop TV" }));
        Assert.Equal("Shop TV", paired.GetProperty("name").GetString());
        Assert.False(paired.GetProperty("online").GetBoolean());

        // the code cannot be used twice, and a shop has one screen
        Assert.Equal(HttpStatusCode.NotFound, (await otherOwner.PostAsJsonAsync($"/api/shops/{otherShop}/screens/pair", new { code, name = "Stolen" })).StatusCode);
        var (second, _) = await TvStartsAsync();
        var tooMany = await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code = second, name = "Second TV" });
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);
        Assert.Contains("already has", await tooMany.Content.ReadAsStringAsync());

        var done = await Json(await _factory.CreateClient().PostAsJsonAsync("/api/screens/pairing/poll", new { deviceSecret = secret }));
        Assert.Equal("paired", done.GetProperty("status").GetString());
        Assert.StartsWith("Screen Shop", done.GetProperty("shopName").GetString());

        // the secret is traded for a token; a wrong secret is refused
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().SendAsync(TokenRequest("not-a-secret"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsync("/api/screens/token", null)).StatusCode);
        var tv = await ScreenClientAsync(secret);

        // what a screen shows: yes
        Assert.Equal(HttpStatusCode.OK, (await tv.GetAsync("/api/banners/active")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await tv.GetAsync($"/api/shops/{shopId}/default-board")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await tv.GetAsync($"/api/shops/{shopId}/ads/live")).StatusCode);

        // another shop: no. Anything else, reading or writing: no
        Assert.Equal(HttpStatusCode.Forbidden, (await tv.GetAsync($"/api/shops/{otherShop}/default-board")).StatusCode);
        foreach (var path in new[] { "/api/banners", "/api/media", "/api/notifications", "/api/account", $"/api/shops/{shopId}/screens", "/api/shop-ads" })
        {
            var refused = await tv.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await tv.PostAsJsonAsync("/api/banners", new { name = "Hack", description = "", width = 100, height = 50 })).StatusCode);
        var restricted = await tv.GetAsync("/api/banners");
        Assert.Contains("screen_restricted", await restricted.Content.ReadAsStringAsync());

        // now it counts as seen
        var listed = (await Json(await owner.GetAsync($"/api/shops/{shopId}/screens"))).EnumerateArray().Single();
        Assert.True(listed.GetProperty("online").GetBoolean());
    }

    [Fact]
    public async Task ACodeThatExpired_CannotBeTyped_AndARemovedScreenLosesItsKey()
    {
        var (owner, shopId, _) = await OwnerAsync();

        var service = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ScreenService>();
        var old = await service.StartPairingAsync(DateTime.UtcNow.AddMinutes(-20));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code = old.Code })).StatusCode);
        Assert.Equal("expired", (await service.PollPairingAsync(old.DeviceSecret)).Status);

        var (code, secret) = await TvStartsAsync();
        var screen = await Json(await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code }));
        Assert.Equal("Shop screen", screen.GetProperty("name").GetString());
        var tv = await ScreenClientAsync(secret);
        var id = screen.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/shops/{shopId}/screens/{id}", new { name = "Window TV" })).StatusCode);
        Assert.Equal("Window TV", (await Json(await owner.GetAsync($"/api/shops/{shopId}/screens"))).EnumerateArray().Single().GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync($"/api/shops/{shopId}/screens/{id}")).StatusCode);

        // the key stops working at once; a token already held cannot report any more
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().SendAsync(TokenRequest(secret))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await tv.PostAsJsonAsync("/api/screens/heartbeat", new { defaultBoard = true })).StatusCode);
        Assert.Empty((await Json(await owner.GetAsync($"/api/shops/{shopId}/screens"))).EnumerateArray());

        // and the shop can add another
        var (again, _) = await TvStartsAsync();
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code = again })).StatusCode);
    }

    [Fact]
    public async Task Heartbeats_CountWhatWasOnTheScreen_AndOnlyWhatBelongsToTheShop()
    {
        var (owner, shopId, _) = await OwnerAsync();
        var (otherOwner, otherShop, _) = await OwnerAsync();
        var (code, secret) = await TvStartsAsync();
        var screen = await Json(await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code }));
        var screenId = screen.GetProperty("id").GetGuid();

        var banner = (await Json(await owner.PostAsJsonAsync("/api/banners", new { name = "Lunch special", description = "", width = 100, height = 50 }))).GetProperty("id").GetGuid();
        var foreignBanner = (await Json(await otherOwner.PostAsJsonAsync("/api/banners", new { name = "Not ours", description = "", width = 100, height = 50 }))).GetProperty("id").GetGuid();
        var adBody = new { advertiserName = "Cafe", headline = "Fresh bread", background = "#ffffff", textColor = "#000000", kind = "Minor", placement = "TopLeft", spacePercent = 0, popupSeconds = 0, popupEveryMinutes = 0, startAt = DateTime.UtcNow.AddDays(-1), endAt = DateTime.UtcNow.AddDays(5) };
        var ad = (await Json(await owner.PostAsJsonAsync("/api/shop-ads", adBody))).GetProperty("id").GetGuid();

        // the service is used here as the API uses it: with the shop of the caller known
        var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IShopContextAccessor>().SetShopId(shopId);
        var service = scope.ServiceProvider.GetRequiredService<ScreenService>();
        var now = DateTime.UtcNow;
        var playing = new HeartbeatInput { BannerId = banner, Ads = new() { new HeartbeatAd { Id = ad } }, DefaultBoard = false };

        await service.HeartbeatAsync(screenId, shopId, playing, "10.0.0.9", now);                       // first one: nothing to count yet
        await service.HeartbeatAsync(screenId, shopId, playing, "10.0.0.9", now.AddSeconds(60));       // 60 s of the banner and the ad
        await service.HeartbeatAsync(screenId, shopId, new HeartbeatInput { DefaultBoard = true }, "10.0.0.9", now.AddSeconds(120)); // then the default board
        await service.HeartbeatAsync(screenId, shopId, new HeartbeatInput { BannerId = foreignBanner }, "10.0.0.9", now.AddSeconds(180)); // someone else's banner: ignored
        await service.HeartbeatAsync(screenId, shopId, playing, "10.0.0.9", now.AddHours(5));          // after a long silence only 150 s are counted

        var report = await Json(await owner.GetAsync($"/api/shops/{shopId}/screens/report?days=7"));
        var rows = report.GetProperty("rows").EnumerateArray().ToDictionary(r => r.GetProperty("label").GetString()!, r => r.GetProperty("hours").GetDecimal());
        Assert.Equal(0.06m, rows["Lunch special"]);  // 60 s + 150 s = 210 s
        Assert.Equal(0.06m, rows["Fresh bread"]);
        Assert.Equal(0.02m, rows["Default board"]);  // 60 s
        Assert.DoesNotContain("Not ours", rows.Keys);
        Assert.Equal(7, report.GetProperty("days").GetArrayLength());

        // the other shop sees none of it
        Assert.Empty((await Json(await otherOwner.GetAsync($"/api/shops/{otherShop}/screens/report?days=7"))).GetProperty("rows").EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.GetAsync($"/api/shops/{shopId}/screens/report")).StatusCode);

        // a screen reports through the API with its token
        var tv = await ScreenClientAsync(secret);
        Assert.Equal(HttpStatusCode.OK, (await tv.PostAsJsonAsync("/api/screens/heartbeat", new { bannerId = banner, ads = new[] { new { id = ad } }, defaultBoard = false, appVersion = "web-1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/screens/heartbeat", new { defaultBoard = true })).StatusCode); // a person cannot pose as a screen
    }

    [Fact]
    public async Task AScreenThatWentQuiet_IsToldToItsOwnerOncePerOutage()
    {
        var (owner, shopId, _) = await OwnerAsync();
        var (code, secret) = await TvStartsAsync();
        var screenId = (await Json(await owner.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code }))).GetProperty("id").GetGuid();
        await ScreenClientAsync(secret); // heard from now

        var scopes = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Monitoring:ScreenOfflineMinutes"] = "10" }).Build();
        var dog = new OpsWatchdog(scopes, config, NullLogger<OpsWatchdog>.Instance);

        async Task<int> NoticesAsync() => (await Json(await owner.GetAsync("/api/notifications?pageSize=50"))).GetProperty("items").EnumerateArray().Count(n => n.GetProperty("kind").GetString() == "ScreenOffline");

        await dog.CheckOnceAsync(DateTime.UtcNow.AddMinutes(5), default);
        Assert.Equal(0, await NoticesAsync()); // five minutes is not long enough

        await dog.CheckOnceAsync(DateTime.UtcNow.AddMinutes(20), default);
        Assert.Equal(1, await NoticesAsync());
        var message = (await Json(await owner.GetAsync("/api/notifications?pageSize=50"))).GetProperty("items").EnumerateArray().First(n => n.GetProperty("kind").GetString() == "ScreenOffline");
        Assert.Equal("/screens", message.GetProperty("linkUrl").GetString());

        await dog.CheckOnceAsync(DateTime.UtcNow.AddMinutes(40), default); // the same outage: not again
        Assert.Equal(1, await NoticesAsync());

        // it came back and went quiet again later: a new notice (after six hours)
        var service = scopes.CreateScope().ServiceProvider.GetRequiredService<ScreenService>();
        await service.HeartbeatAsync(screenId, shopId, new HeartbeatInput { DefaultBoard = true }, null, DateTime.UtcNow.AddMinutes(41));
        await dog.CheckOnceAsync(DateTime.UtcNow.AddHours(8), default);
        Assert.Equal(2, await NoticesAsync());
    }

    [Fact]
    public async Task OnlyOwnersPair_AndExecutivesCanOnlyLook()
    {
        var (owner, shopId, _) = await OwnerAsync();
        var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var exec = new User
        {
            Email = $"screen-exec-{Guid.NewGuid():N}@example.com", FirstName = "Eli", LastName = "Exec", ShopId = shopId.ToString(), IsActive = true,
            PasswordHash = new PasswordHashService().HashPassword("Password123!"),
        };
        context.Users.Add(exec);
        context.UserRoles.Add(new UserRole { UserId = exec.Id, RoleId = "3" });
        await context.SaveChangesAsync();

        var client = _factory.CreateClient();
        var login = await Json(await client.PostAsJsonAsync("/api/authentication/login", new { email = exec.Email, password = "Password123!" }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("tokens").GetProperty("accessToken").GetString()!);

        var (code, _) = await TvStartsAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/shops/{shopId}/screens/pair", new { code })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/shops/{shopId}/screens")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/shops/{shopId}/screens/report")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/shops/{shopId}/screens")).StatusCode);
    }
}
