using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>Booking ads on shop screens: who may book, approval, clashes, what the admin sees, what is live, history.</summary>
public class ShopAdSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public ShopAdSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<(HttpClient client, Guid shopId)> OwnerAsync(string email)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Ada", lastName = "Ads", shopName = "Ad Shop " + email });
        var token = (await Json(response)).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value));
    }

    private async Task<HttpClient> ExecutiveAsync(Guid shopId, string email)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = new User
            {
                Email = email, FirstName = "Sam", LastName = "Seller", ShopId = shopId.ToString(), IsActive = true,
                PasswordHash = new PasswordHashService().HashPassword("Password123!"),
            };
            context.Users.Add(user);
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "3" });
            await context.SaveChangesAsync();
        }
        return await LoginAsync(email, "Password123!");
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/authentication/login", new { email, password });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        return client;
    }

    private async Task<HttpClient> AdminAsync()
    {
        await _factory.SeedAsync();
        return await LoginAsync("admin@example.com", "AdminPass123!");
    }

    /// <summary>Administrator ads are priced by a rate; make sure one exists for every shop (a conflict means it is there already).</summary>
    private static async Task EnsureRateAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/ad-rates", new { level = "All", pricePerHour = 100, shopSharePercent = 70 });
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict);
    }

    private static object Ad(string headline, string kind = "Side", string placement = "Left", int percent = 20, int fromDay = 1, int days = 7, object? daily = null, Guid? shopId = null) => new
    {
        shopId, advertiserName = "Olive Cafe", headline, body = "Fresh coffee", background = "#ffeecc", textColor = "#112233",
        kind, placement, spacePercent = percent, popupSeconds = kind == "Popup" ? 10 : 0, popupEveryMinutes = kind == "Popup" ? 5 : 0,
        startAt = DateTime.UtcNow.Date.AddDays(fromDay), endAt = DateTime.UtcNow.Date.AddDays(fromDay + days), daily,
    };

    private static async Task<Guid> CreateAsync(HttpClient client, object ad)
    {
        var response = await client.PostAsJsonAsync("/api/shop-ads", ad);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await Json(response)).GetProperty("id").GetGuid();
    }

    private static async Task<string> StatusAfter(HttpClient client, Guid id, string action, object? body = null)
    {
        var response = await client.PostAsJsonAsync($"/api/shop-ads/{id}/{action}", body ?? new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await Json(response)).GetProperty("status").GetString()!;
    }

    [Fact]
    public async Task OwnerAdIsApprovedAtOnce_ExecutiveAdWaitsForTheOwner_AndTheOwnerIsToldAndDecides()
    {
        var (owner, shopId) = await OwnerAsync("ads-owner1@example.com");
        var exec = await ExecutiveAsync(shopId, "ads-exec1@example.com");

        var own = await CreateAsync(owner, Ad("Owner offer", placement: "Left"));
        Assert.Equal("Approved", await StatusAfter(owner, own, "submit"));

        var theirs = await CreateAsync(exec, Ad("Exec offer", placement: "Right"));
        Assert.Equal("PendingApproval", await StatusAfter(exec, theirs, "submit"));

        var inbox = await Json(await owner.GetAsync("/api/notifications"));
        Assert.Contains(inbox.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "AdSubmitted" && n.GetProperty("message").GetString()!.Contains("Exec offer"));

        // the executive cannot approve, and the owner needs a reason to send one back
        Assert.Equal(HttpStatusCode.Forbidden, (await exec.PostAsJsonAsync($"/api/shop-ads/{theirs}/approve", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/api/shop-ads/{theirs}/reject", new { })).StatusCode);

        Assert.Equal("Approved", await StatusAfter(owner, theirs, "approve", new { note = "Looks good" }));
        var told = await Json(await exec.GetAsync("/api/notifications"));
        Assert.Contains(told.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "AdApproved");

        var history = await Json(await owner.GetAsync($"/api/shop-ads/{theirs}/history"));
        Assert.Equal(new[] { "Created", "Submitted", "Approved" }, history.EnumerateArray().Select(e => e.GetProperty("action").GetString()!));
        Assert.Equal("Looks good", history[2].GetProperty("note").GetString());
    }

    [Fact]
    public async Task ARejectedAdCanBeChangedAndSentAgain()
    {
        var (owner, shopId) = await OwnerAsync("ads-owner2@example.com");
        var exec = await ExecutiveAsync(shopId, "ads-exec2@example.com");
        var id = await CreateAsync(exec, Ad("Too loud", percent: 20));
        await StatusAfter(exec, id, "submit");

        Assert.Equal("Rejected", await StatusAfter(owner, id, "reject", new { reason = "Please tone the headline down" }));
        var edited = await exec.PutAsJsonAsync($"/api/shop-ads/{id}", Ad("A calmer headline", percent: 20));
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal("Draft", (await Json(edited)).GetProperty("status").GetString());
        Assert.Equal("PendingApproval", await StatusAfter(exec, id, "submit"));

        // an approved ad can no longer be edited, only cancelled
        await StatusAfter(owner, id, "approve");
        Assert.Equal(HttpStatusCode.Conflict, (await exec.PutAsJsonAsync($"/api/shop-ads/{id}", Ad("Sneaky change"))).StatusCode);
        Assert.Equal("Cancelled", await StatusAfter(exec, id, "cancel"));
    }

    [Fact]
    public async Task AdsThatWouldShareTheScreenAtTheSameTime_AreRefused_WithTheReason()
    {
        var (owner, _) = await OwnerAsync("ads-owner3@example.com");
        var first = await CreateAsync(owner, Ad("Big sale", "Mega", "Left", 60));
        Assert.Equal("Approved", await StatusAfter(owner, first, "submit"));

        var clash = await CreateAsync(owner, Ad("Small strip", "Side", "Right", 20));
        var refused = await owner.PostAsJsonAsync($"/api/shop-ads/{clash}/submit", new { });
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("Big sale", (await Json(refused)).GetProperty("message").GetString());

        // after the big one is cancelled the strip fits
        await StatusAfter(owner, first, "cancel");
        Assert.Equal("Approved", await StatusAfter(owner, clash, "submit"));
    }

    [Fact]
    public async Task BadAds_AreRefusedAtOnce_AndAnotherShopsAdsAreNotFound()
    {
        var (owner, _) = await OwnerAsync("ads-owner4@example.com");
        var (other, _) = await OwnerAsync("ads-owner5@example.com");

        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/shop-ads", Ad("Too big", "Side", "Left", 90))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/shop-ads", Ad("", "Side", "Left", 20))).StatusCode);

        var mine = await CreateAsync(owner, Ad("Mine"));
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/shop-ads/{mine}/submit", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/shop-ads/{mine}/history")).StatusCode);

        // the admin names the shop; an owner naming someone else's is refused
        var (_, otherShop) = await OwnerAsync("ads-owner6@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/shop-ads", Ad("Elsewhere", shopId: otherShop))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/shop-ads")).StatusCode);
    }

    [Fact]
    public async Task AnAdminAd_IsApprovedAtOnce_TellsTheOwner_CannotBeCancelledByThem_AndShowsToTheAdminWithTheOwnersBookings()
    {
        var (owner, shopId) = await OwnerAsync("ads-owner7@example.com");
        var admin = await AdminAsync();

        // the admin has to name the shop, and cannot attach a picture (the admin has no files of the shop)
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/shop-ads", Ad("No shop"))).StatusCode);

        await EnsureRateAsync(admin);
        var adminAd = await CreateAsync(admin, Ad("Admin deal", "Side", "Right", 20, shopId: shopId));
        Assert.Equal("Approved", await StatusAfter(admin, adminAd, "submit"));

        var told = await Json(await owner.GetAsync("/api/notifications"));
        Assert.Contains(told.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "AdBookedByAdmin");

        // the owner sees it but can only override it, not change or cancel it
        var ownerList = await Json(await owner.GetAsync("/api/shop-ads"));
        var seen = ownerList.EnumerateArray().Single(a => a.GetProperty("id").GetGuid() == adminAd);
        Assert.Equal(new[] { "override" }, seen.GetProperty("can").EnumerateArray().Select(c => c.GetString()!));
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync($"/api/shop-ads/{adminAd}/cancel", new { })).StatusCode);

        // the owner books another slot; the admin sees both as booked
        var ownersAd = await CreateAsync(owner, Ad("Owner slot", "Side", "Left", 20));
        await StatusAfter(owner, ownersAd, "submit");
        var booked = await Json(await admin.GetAsync($"/api/shop-ads?shopId={shopId}"));
        Assert.Equal(new[] { "Admin", "ShopOwner" }, booked.EnumerateArray().Select(a => a.GetProperty("source").GetString()!).OrderBy(x => x));

        Assert.Equal("Cancelled", await StatusAfter(admin, adminAd, "cancel"));
    }

    [Fact]
    public async Task OnlyApprovedAdsInTheirHoursAreLive_AndAShopOnlySeesItsOwn()
    {
        var (owner, shopId) = await OwnerAsync("ads-owner8@example.com");
        var (other, _) = await OwnerAsync("ads-owner9@example.com");

        var live = await CreateAsync(owner, Ad("Live now", "Minor", "TopLeft", fromDay: -1, days: 3));
        await StatusAfter(owner, live, "submit");
        var later = await CreateAsync(owner, Ad("Next week", "Minor", "TopRight", fromDay: 7));
        await StatusAfter(owner, later, "submit");
        await CreateAsync(owner, Ad("Draft only", "Minor", "BottomLeft", fromDay: -1, days: 3));

        var ads = await Json(await owner.GetAsync($"/api/shops/{shopId}/ads/live"));

        Assert.Equal(new[] { "Live now" }, ads.EnumerateArray().Select(a => a.GetProperty("headline").GetString()!));
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/shops/{shopId}/ads/live")).StatusCode);
    }

    [Fact]
    public async Task APictureOnALiveAdCannotBeDeletedFromTheLibrary()
    {
        var (owner, _) = await OwnerAsync("ads-owner10@example.com");
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 2, 0, 0, 0, 2, 8, 6, 0, 0, 0, 1, 2, 3, 4 };
        var init = await Json(await owner.PostAsJsonAsync("/api/media/upload/initialize", new { fileName = "ad.png", contentType = "image/png", totalSizeBytes = png.Length, fileType = 1 }));
        var mediaId = init.GetProperty("mediaFileId").GetGuid();
        var chunk = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{mediaId}/chunks/0") { Content = new ByteArrayContent(png) };
        chunk.Headers.Add("X-Checksum-MD5", Convert.ToHexString(System.Security.Cryptography.MD5.HashData(png)).ToLowerInvariant());
        Assert.Equal(HttpStatusCode.OK, (await owner.SendAsync(chunk)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/media/{mediaId}/complete", null)).StatusCode);

        var withPicture = Ad("Picture ad", "Minor", "TopLeft", fromDay: 1, days: 5);
        var json = JsonSerializer.SerializeToElement(withPicture);
        var body = JsonSerializer.Deserialize<Dictionary<string, object?>>(json.GetRawText())!;
        body["mediaFileId"] = mediaId;
        var created = await owner.PostAsJsonAsync("/api/shop-ads", body);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await Json(created)).GetProperty("id").GetGuid();
        await StatusAfter(owner, id, "submit");

        var refused = await owner.DeleteAsync($"/api/media/{mediaId}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("Picture ad", await refused.Content.ReadAsStringAsync());

        await StatusAfter(owner, id, "cancel");
        Assert.True((await owner.DeleteAsync($"/api/media/{mediaId}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task TheOwnerCanOverrideAnAdminAd_TheAdminIsToldAndTheSlotIsFree()
    {
        var (owner, shopId) = await OwnerAsync("ads-owner11@example.com");
        var exec = await ExecutiveAsync(shopId, "ads-exec11@example.com");
        var admin = await AdminAsync();
        await EnsureRateAsync(admin);

        var adminAd = await CreateAsync(admin, Ad("Admin deal", "Mega", "Left", 60, fromDay: 1, shopId: shopId));
        await StatusAfter(admin, adminAd, "submit");

        // the owner has a better local offer for the same time, but the screen is taken
        var local = await CreateAsync(owner, Ad("Local deal", "Mega", "Right", 60));
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/shop-ads/{local}/submit", new { })).StatusCode);

        // an executive cannot override, an owner of another shop cannot see it
        Assert.Equal(HttpStatusCode.Forbidden, (await exec.PostAsJsonAsync($"/api/shop-ads/{adminAd}/override", new { reason = "x" })).StatusCode);
        var (other, _) = await OwnerAsync("ads-owner12@example.com");
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/shop-ads/{adminAd}/override", new { })).StatusCode);

        var seen = (await Json(await owner.GetAsync("/api/shop-ads"))).EnumerateArray().Single(a => a.GetProperty("id").GetGuid() == adminAd);
        Assert.Contains("override", seen.GetProperty("can").EnumerateArray().Select(c => c.GetString()));
        Assert.Equal(60m, (await Json(await admin.GetAsync($"/api/shop-ads?shopId={shopId}"))).EnumerateArray().Single(a => a.GetProperty("id").GetGuid() == adminAd).GetProperty("pricePerHour").GetDecimal()); // 60% of 100

        Assert.Equal("Overridden", await StatusAfter(owner, adminAd, "override", new { reason = "A local shop pays more" }));

        var told = await Json(await admin.GetAsync("/api/notifications"));
        var message = told.GetProperty("items").EnumerateArray().First(n => n.GetProperty("kind").GetString() == "AdOverridden");
        Assert.Contains("A local shop pays more", message.GetProperty("message").GetString());
        Assert.Contains("Admin deal", message.GetProperty("message").GetString());

        Assert.Equal("Approved", await StatusAfter(owner, local, "submit"));
        var history = await Json(await admin.GetAsync($"/api/shop-ads/{adminAd}/history"));
        Assert.Equal("Overridden", history.EnumerateArray().Last().GetProperty("action").GetString());

        // it cannot be overridden twice
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/shop-ads/{adminAd}/override", new { })).StatusCode);
    }

    [Fact]
    public async Task AdRates_AreTheAdministratorsAlone_AreCheckedAndNeverDeleted()
    {
        var (owner, _) = await OwnerAsync("ads-owner13@example.com");
        var admin = await AdminAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/ad-rates")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/ad-rates", new { level = "All", pricePerHour = 1 })).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/ad-rates", new { level = "All", pricePerHour = -5 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/ad-rates", new { level = "City", pricePerHour = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/ad-rates", new { level = "City", cityId = 999999, pricePerHour = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/ad-rates", new { level = "Country", countryCode = "ZZ", pricePerHour = 5 })).StatusCode);

        // a rate for one kind in one place, once
        var kind = "Minor";
        var first = await admin.PostAsJsonAsync("/api/ad-rates", new { level = "All", kind, pricePerHour = 15.5, shopSharePercent = 60 });
        if (first.StatusCode == HttpStatusCode.OK)
        {
            var id = (await Json(first)).GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/ad-rates", new { level = "All", kind, pricePerHour = 20 })).StatusCode);

            var changed = await Json(await admin.PutAsJsonAsync($"/api/ad-rates/{id}", new { level = "All", kind, pricePerHour = 18, shopSharePercent = 65 }));
            Assert.Equal(18m, changed.GetProperty("pricePerHour").GetDecimal());

            var off = await Json(await admin.DeleteAsync($"/api/ad-rates/{id}"));
            Assert.False(off.GetProperty("isActive").GetBoolean());
            Assert.Contains((await Json(await admin.GetAsync("/api/ad-rates"))).EnumerateArray(), r => r.GetProperty("id").GetGuid() == id);
        }
    }

    [Fact]
    public async Task TheMonthlyStatement_IsForOwnersAndAdmins_WithARealMonth()
    {
        var (owner, shopId) = await OwnerAsync("ads-owner14@example.com");
        var exec = await ExecutiveAsync(shopId, "ads-exec14@example.com");
        var admin = await AdminAsync();
        var month = DateTime.UtcNow.ToString("yyyy-MM");

        Assert.Equal(HttpStatusCode.Forbidden, (await exec.GetAsync($"/api/ad-statements?month={month}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/api/ad-statements?month=soon")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/api/ad-statements?month={month}&shopId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/ad-statements?month={month}")).StatusCode);

        var own = await Json(await owner.GetAsync($"/api/ad-statements?month={month}"));
        Assert.Equal(month, own.GetProperty("month").GetString());
        Assert.Equal(0m, own.GetProperty("totalPayout").GetDecimal());

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/ad-statements?month={month}")).StatusCode);
    }

    [Fact]
    public async Task ACampaign_BooksEveryShopInACity_SkipsTheOnesItCannotGoOn_AndTellsTheOwners()
    {
        int stateId;
        using (var scope = _factory.Services.CreateScope())
        {
            await _factory.SeedAsync();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await context.Countries.AnyAsync(c => c.ISOCode == "IN")) context.Countries.Add(new Country { ISOCode = "IN", Name = "India" });
            var state = await context.States.FirstOrDefaultAsync(x => x.CountryCode == "IN" && x.Code == "ZT");
            if (state == null) context.States.Add(state = new State { CountryCode = "IN", Code = "ZT", Name = "Zone Test State" });
            await context.SaveChangesAsync();
            stateId = state.Id;
        }

        var admin = await AdminAsync();
        await EnsureRateAsync(admin);
        var city = await Json(await admin.PostAsJsonAsync("/api/locations/cities", new { stateId, name = "Campaign City " + Guid.NewGuid().ToString("N")[..6] }));
        var cityId = int.Parse(city.GetProperty("id").GetString()!);

        var (free, freeShop) = await OwnerAsync("camp-free@example.com");
        var (busy, busyShop) = await OwnerAsync("camp-busy@example.com");
        var (elsewhere, elsewhereShop) = await OwnerAsync("camp-elsewhere@example.com");
        await free.PutAsJsonAsync($"/api/locations/shops/{freeShop}", new { cityId });
        await busy.PutAsJsonAsync($"/api/locations/shops/{busyShop}", new { cityId });

        // one shop already has a mega ad at that time
        var taken = await CreateAsync(busy, Ad("Already here", "Mega", "Left", 60));
        await StatusAfter(busy, taken, "submit");

        Assert.Equal(HttpStatusCode.Forbidden, (await free.PostAsJsonAsync("/api/shop-ads/campaign", Ad("Nope", "Mega", "Right", 60))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/shop-ads/campaign", new { cityId = 999999, advertiserName = "Big Brand", headline = "Hi", background = "#ffffff", textColor = "#000000", kind = "Side", placement = "Left", spacePercent = 20, startAt = DateTime.UtcNow.AddDays(1), endAt = DateTime.UtcNow.AddDays(5) })).StatusCode);

        var body = new Dictionary<string, object?>
        {
            ["cityId"] = cityId, ["advertiserName"] = "Big Brand", ["headline"] = "Festival sale", ["background"] = "#ffeecc", ["textColor"] = "#112233",
            ["kind"] = "Mega", ["placement"] = "Right", ["spacePercent"] = 60, ["startAt"] = DateTime.UtcNow.Date.AddDays(1), ["endAt"] = DateTime.UtcNow.Date.AddDays(8),
        };
        var response = await admin.PostAsJsonAsync("/api/shop-ads/campaign", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await Json(response);

        Assert.Equal(2, result.GetProperty("shops").GetInt32());
        Assert.Equal(1, result.GetProperty("booked").GetInt32());
        var skipped = Assert.Single(result.GetProperty("skipped").EnumerateArray());
        Assert.Equal(busyShop, skipped.GetProperty("shopId").GetGuid());
        Assert.Contains("Already here", skipped.GetProperty("reason").GetString());

        // the booked shop got the ad and a message saying it is a major ad; the other places were left alone
        var ads = await Json(await free.GetAsync("/api/shop-ads"));
        Assert.Equal("Approved", ads.EnumerateArray().Single(a => a.GetProperty("headline").GetString() == "Festival sale").GetProperty("status").GetString());
        var told = await Json(await free.GetAsync("/api/notifications"));
        Assert.Contains(told.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "AdBookedByAdmin" && n.GetProperty("title").GetString()!.StartsWith("A major ad"));
        Assert.DoesNotContain((await Json(await elsewhere.GetAsync("/api/shop-ads"))).EnumerateArray(), a => a.GetProperty("headline").GetString() == "Festival sale");
        Assert.DoesNotContain((await Json(await busy.GetAsync("/api/shop-ads"))).EnumerateArray(), a => a.GetProperty("headline").GetString() == "Festival sale" && a.GetProperty("status").GetString() != "Cancelled");
    }
}
