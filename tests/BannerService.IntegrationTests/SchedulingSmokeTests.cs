using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>The shop time zone, banners with daily hours, the calendar and the shop's own default board, through the real application.</summary>
public class SchedulingSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public SchedulingSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid shopId)> OwnerAsync(string email)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Sky", lastName = "Schedule", shopName = "Schedule Shop " + email });
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var shopId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value);
        return (client, shopId);
    }

    private static async Task<Guid> NewBannerAsync(HttpClient client, string name)
    {
        var banner = await (await client.PostAsJsonAsync("/api/banners", new { name, description = "", width = 100, height = 50 })).Content.ReadFromJsonAsync<JsonElement>();
        return banner.GetProperty("id").GetGuid();
    }

    private static readonly DateTime Monday = new(2030, 1, 7, 0, 0, 0, DateTimeKind.Utc);

    private static object Schedule(int? fromMinutes = null, int? toMinutes = null, int? days = null, int weeks = 1) =>
        new { startAt = Monday.AddHours(-5.5), endAt = Monday.AddDays(7 * weeks).AddHours(-5.5), dailyStartMinutes = fromMinutes, dailyEndMinutes = toMinutes, activeDays = days };

    private static Task<HttpResponseMessage> SetSchedule(HttpClient client, Guid bannerId, object body) =>
        client.PutAsJsonAsync($"/api/banners/{bannerId}/schedule", body);

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    // ----- time zone

    [Fact]
    public async Task TheShopTimeZone_CanBeSet_Changed_AndCleared()
    {
        var (client, shopId) = await OwnerAsync("tz1@example.com");

        var start = await Json(await client.GetAsync($"/api/shops/{shopId}/time-zone"));
        Assert.Equal("UTC", start.GetProperty("timeZoneId").GetString());
        Assert.Equal("default", start.GetProperty("source").GetString());

        var set = await Json(await client.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "Asia/Kolkata" }));
        Assert.Equal("Asia/Kolkata", set.GetProperty("timeZoneId").GetString());
        Assert.Equal("shop", set.GetProperty("source").GetString());

        var shop = await Json(await client.GetAsync($"/api/shops/{shopId}"));
        Assert.Equal("Asia/Kolkata", shop.GetProperty("timeZoneId").GetString());
        Assert.Equal("Asia/Kolkata", shop.GetProperty("ownTimeZoneId").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "Mars/Olympus" })).StatusCode);

        var cleared = await Json(await client.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = (string?)null }));
        Assert.Equal("default", cleared.GetProperty("source").GetString());
    }

    [Fact]
    public async Task AShopFollowsItsCitysTimeZone_UntilItSetsItsOwn_AndOtherShopsCannotChangeIt()
    {
        var (owner, shopId) = await OwnerAsync("tz2@example.com");
        var (intruder, _) = await OwnerAsync("tz3@example.com");
        int stateId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await context.Countries.AnyAsync(c => c.ISOCode == "IN"))
                context.Countries.Add(new Country { ISOCode = "IN", Name = "India" });
            var state = await context.States.FirstOrDefaultAsync(s => s.CountryCode == "IN" && s.Code == "ZT");
            if (state == null)
                context.States.Add(state = new State { CountryCode = "IN", Code = "ZT", Name = "Zone Test State" });
            await context.SaveChangesAsync();
            stateId = state.Id;
        }

        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);

        var city = await Json(await admin.PostAsJsonAsync("/api/locations/cities", new { stateId, name = "Zone City " + Guid.NewGuid().ToString("N")[..6] }));
        var cityId = int.Parse(city.GetProperty("id").GetString()!);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/api/locations/cities/{cityId}/time-zone", new { timeZoneId = "Asia/Kolkata" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/locations/cities/{cityId}/time-zone", new { timeZoneId = "Mars/Olympus" })).StatusCode);
        var withZone = await Json(await admin.PutAsJsonAsync($"/api/locations/cities/{cityId}/time-zone", new { timeZoneId = "Asia/Kolkata" }));
        Assert.Equal("Asia/Kolkata", withZone.GetProperty("timeZoneId").GetString());

        await owner.PutAsJsonAsync($"/api/locations/shops/{shopId}", new { cityId });
        var follows = await Json(await owner.GetAsync($"/api/shops/{shopId}/time-zone"));
        Assert.Equal(("Asia/Kolkata", "city"), (follows.GetProperty("timeZoneId").GetString(), follows.GetProperty("source").GetString()));

        await owner.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "Europe/London" });
        var own = await Json(await owner.GetAsync($"/api/shops/{shopId}/time-zone"));
        Assert.Equal(("Europe/London", "shop"), (own.GetProperty("timeZoneId").GetString(), own.GetProperty("source").GetString()));

        Assert.Equal(HttpStatusCode.Forbidden, (await intruder.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "UTC" })).StatusCode);
    }

    // ----- daily hours

    [Fact]
    public async Task BannersCanShareDatesWhenTheirHoursDoNotMeet()
    {
        var (client, shopId) = await OwnerAsync("hours1@example.com");
        await client.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "Asia/Kolkata" });
        var morning = await NewBannerAsync(client, "Morning");
        var afternoon = await NewBannerAsync(client, "Afternoon");
        var clash = await NewBannerAsync(client, "Clash");

        var first = await SetSchedule(client, morning, Schedule(9 * 60, 12 * 60));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var saved = await Json(first);
        Assert.Equal(9 * 60, saved.GetProperty("dailyStartMinutes").GetInt32());
        Assert.Equal("Asia/Kolkata", saved.GetProperty("timeZoneId").GetString());
        Assert.Equal(127, saved.GetProperty("activeDays").GetInt32());

        Assert.Equal(HttpStatusCode.OK, (await SetSchedule(client, afternoon, Schedule(12 * 60, 15 * 60))).StatusCode);

        var refused = await SetSchedule(client, clash, Schedule(11 * 60, 13 * 60));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        var reason = (await Json(refused)).GetProperty("message").GetString()!;
        Assert.True(reason.Contains("Morning") || reason.Contains("Afternoon"), reason);

        // a banner for the whole window clashes with both
        Assert.Equal(HttpStatusCode.Conflict, (await SetSchedule(client, clash, Schedule())).StatusCode);
    }

    [Fact]
    public async Task WeekdaysDecideWhetherSameHoursClash()
    {
        var (client, shopId) = await OwnerAsync("hours2@example.com");
        await client.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "Asia/Kolkata" });
        var weekdays = await NewBannerAsync(client, "Weekdays");
        var weekend = await NewBannerAsync(client, "Weekend");
        var friday = await NewBannerAsync(client, "Friday");
        const int mondayToFriday = 2 | 4 | 8 | 16 | 32, saturdayAndSunday = 64 | 1, fridayOnly = 32;

        Assert.Equal(HttpStatusCode.OK, (await SetSchedule(client, weekdays, Schedule(9 * 60, 17 * 60, mondayToFriday))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SetSchedule(client, weekend, Schedule(9 * 60, 17 * 60, saturdayAndSunday))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SetSchedule(client, friday, Schedule(9 * 60, 17 * 60, fridayOnly))).StatusCode);
    }

    [Fact]
    public async Task BadDailyHours_AreRefused_WithAReason()
    {
        var (client, _) = await OwnerAsync("hours3@example.com");
        var banner = await NewBannerAsync(client, "Bad");

        foreach (var body in new object[]
                 {
                     Schedule(600, 600),             // the same start and end
                     Schedule(-5, 600),
                     Schedule(600, 1440),
                     Schedule(600, 700, 0),          // no day chosen
                     Schedule(600, null),            // one end only
                 })
        {
            var response = await SetSchedule(client, banner, body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace((await Json(response)).GetProperty("message").GetString()));
        }
    }

    [Fact]
    public async Task ChangingTheHoursOfAnApprovedBanner_SendsItBackForApproval()
    {
        var (client, _) = await OwnerAsync("hours4@example.com");
        var banner = await NewBannerAsync(client, "Needs approval");
        Assert.False((await Json(await SetSchedule(client, banner, Schedule(9 * 60, 12 * 60)))).GetProperty("requiresReapproval").GetBoolean());

        // an approved or live banner is the only kind that goes back; make sure the flag answers false when nothing changed
        var same = await Json(await SetSchedule(client, banner, Schedule(9 * 60, 12 * 60)));
        Assert.False(same.GetProperty("requiresReapproval").GetBoolean());
    }

    // ----- calendar

    [Fact]
    public async Task TheCalendar_ListsEachStretchInTheShopsTimeZone()
    {
        var (client, shopId) = await OwnerAsync("calendar1@example.com");
        await client.PutAsJsonAsync($"/api/shops/{shopId}/time-zone", new { timeZoneId = "Asia/Kolkata" });
        var morning = await NewBannerAsync(client, "Morning");
        var allDay = await NewBannerAsync(client, "Next week");
        await SetSchedule(client, morning, Schedule(9 * 60, 12 * 60));
        await SetSchedule(client, allDay, new { startAt = Monday.AddDays(7), endAt = Monday.AddDays(9) });

        var from = Monday.AddDays(-1).ToString("o");
        var to = Monday.AddDays(14).ToString("o");
        var calendar = await Json(await client.GetAsync($"/api/banners/schedule/calendar?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}"));

        Assert.Equal("Asia/Kolkata", calendar.GetProperty("timeZoneId").GetString());
        var entries = calendar.GetProperty("entries").EnumerateArray().ToList();
        Assert.Equal(7, entries.Count(e => e.GetProperty("name").GetString() == "Morning"));
        var first = entries.First(e => e.GetProperty("name").GetString() == "Morning");
        Assert.Equal(Monday.AddHours(3.5), first.GetProperty("startUtc").GetDateTime().ToUniversalTime());   // 09:00 in Kolkata
        Assert.Equal(Monday.AddHours(6.5), first.GetProperty("endUtc").GetDateTime().ToUniversalTime());
        Assert.False(first.GetProperty("published").GetBoolean());
        Assert.Single(entries, e => e.GetProperty("name").GetString() == "Next week");
    }

    [Fact]
    public async Task TheCalendar_RefusesAnEmptyOrTooLongRange_AndNeedsSignIn()
    {
        var (client, _) = await OwnerAsync("calendar2@example.com");
        var from = Monday.ToString("o");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/banners/schedule/calendar?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(from)}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/banners/schedule/calendar?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(Monday.AddDays(90).ToString("o"))}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/banners/schedule/calendar")).StatusCode);
    }

    // ----- default board

    private static byte[] Png(int length)
    {
        var bytes = new byte[length];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        return bytes;
    }

    private static async Task<Guid> UploadAsync(HttpClient client, string name, byte[] bytes)
    {
        var init = await client.PostAsJsonAsync("/api/media/upload/initialize", new { fileName = name, contentType = "image/png", totalSizeBytes = bytes.Length, fileType = 1 });
        var id = (await Json(init)).GetProperty("mediaFileId").GetGuid();
        var chunk = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{id}/chunks/0") { Content = new ByteArrayContent(bytes) };
        chunk.Headers.Add("X-Checksum-MD5", Convert.ToHexString(System.Security.Cryptography.MD5.HashData(bytes)).ToLowerInvariant());
        await client.SendAsync(chunk);
        await client.PostAsync($"/api/media/{id}/complete", null);
        return id;
    }

    [Fact]
    public async Task TheDefaultBoard_IsDesignedByTheOwner_AndTheLogoCannotBeDeletedWhileInUse()
    {
        var (owner, shopId) = await OwnerAsync("board1@example.com");
        var logo = await UploadAsync(owner, "logo.png", Png(2000));

        var empty = await Json(await owner.GetAsync($"/api/shops/{shopId}/default-board"));
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("message").ValueKind);
        Assert.Contains("Schedule Shop", empty.GetProperty("shopName").GetString());

        var saved = await owner.PutAsJsonAsync($"/api/shops/{shopId}/default-board",
            new { message = "Back at 2 pm", background = "#1E3A8A", textColor = "#FFFFFF", logoMediaFileId = logo });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var board = await Json(await owner.GetAsync($"/api/shops/{shopId}/default-board"));
        Assert.Equal("Back at 2 pm", board.GetProperty("message").GetString());
        Assert.Equal("#1e3a8a", board.GetProperty("background").GetString());
        var logoUrl = board.GetProperty("logoUrl").GetString()!;
        Assert.True((await _factory.CreateClient().GetAsync(logoUrl)).IsSuccessStatusCode);

        // the logo is in use: the library refuses to delete it and says why
        var refused = await owner.DeleteAsync($"/api/media/{logo}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("default board", (await Json(refused)).GetProperty("message").GetString());

        // take it off and the file can go
        await owner.PutAsJsonAsync($"/api/shops/{shopId}/default-board", new { message = "Back at 2 pm" });
        Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync($"/api/media/{logo}")).StatusCode);
    }

    [Fact]
    public async Task TheDefaultBoard_RefusesBadColoursAndOtherShopsFiles_AndOnlyTheOwnerChangesIt()
    {
        var (owner, shopId) = await OwnerAsync("board2@example.com");
        var (other, _) = await OwnerAsync("board3@example.com");
        var foreign = await UploadAsync(other, "theirs.png", Png(1500));

        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/shops/{shopId}/default-board", new { background = "blue" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/shops/{shopId}/default-board", new { logoMediaFileId = foreign })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/shops/{shopId}/default-board", new { message = new string('x', 201) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/shops/{shopId}/default-board", new { message = "hijack" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/shops/{shopId}/default-board")).StatusCode);
    }
}
