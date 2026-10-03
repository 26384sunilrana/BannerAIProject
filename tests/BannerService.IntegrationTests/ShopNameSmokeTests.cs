using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>
/// Shop names may repeat when the addresses differ; one name at one address is one shop, and a shop that changes hands goes through a
/// takeover that both owners (or an administrator for the old one) agree to.
/// </summary>
public class ShopNameSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public ShopNameSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private static string Tag() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    private sealed record Owner(HttpClient Client, Guid ShopId, string UserId, string Email);

    private async Task<Owner> OwnerAsync(string email, string shopName)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Sam", lastName = "Same", shopName });
        if (!response.IsSuccessStatusCode) throw new Exception("register failed " + (int)response.StatusCode + ": " + await response.Content.ReadAsStringAsync());
        var token = (await Json(response)).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return new Owner(client, Guid.Parse(jwt.Claims.First(c => c.Type == "shop_id").Value), jwt.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value, email);
    }

    private async Task<HttpClient> LoginAsync(string email, string password = "Password123!")
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/authentication/login", new { email, password });
        if (login.StatusCode != HttpStatusCode.OK) return client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        return client;
    }

    private async Task<HttpClient> AdminAsync()
    {
        await _factory.SeedAsync();
        return await LoginAsync("admin@example.com", "AdminPass123!");
    }

    private static Task<HttpResponseMessage> SetAddress(Owner owner, string name, string address, string? postal = "400001") =>
        owner.Client.PutAsJsonAsync($"/api/shops/{owner.ShopId}", new { name, address, postalCode = postal, status = 1 });

    private async Task<string> AddExecutiveAsync(Guid shopId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Email = email, FirstName = "Eli", LastName = "Exec", ShopId = shopId.ToString(), IsActive = true,
            PasswordHash = new PasswordHashService().HashPassword("Password123!"),
        };
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "3" });
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static Task<HttpResponseMessage> Ask(Owner by, string name, string address, string? postal = "400001") =>
        by.Client.PostAsJsonAsync("/api/shop-takeovers", new { name, address, postalCode = postal });

    // ----- names and addresses

    [Fact]
    public async Task TwoShopsMayShareAName_WhenTheirAddressesDiffer_ButNotNameAndAddressTogether()
    {
        var name = "Same Name Shop " + Tag();
        var first = await OwnerAsync($"sn1-{Tag()}@example.com", name);
        var second = await OwnerAsync($"sn2-{Tag()}@example.com", name); // sign-up with the same name is fine

        Assert.Equal(HttpStatusCode.OK, (await SetAddress(first, name, "12 Main Road")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SetAddress(second, name, "99 Station Road")).StatusCode);

        // the same name at the first shop's address is refused, however it is written
        var clash = await SetAddress(second, name.ToUpperInvariant(), "12, main road.");
        Assert.Equal(HttpStatusCode.BadRequest, clash.StatusCode);
        Assert.Contains("takeover", (await Json(clash)).GetProperty("message").GetString());

        // a shop can keep saving its own details
        Assert.Equal(HttpStatusCode.OK, (await first.Client.PutAsJsonAsync($"/api/shops/{first.ShopId}", new { name, address = "12 Main Road", postalCode = "400001", description = "Open late", status = 1 })).StatusCode);

        // a different postal code makes it a different place
        Assert.Equal(HttpStatusCode.OK, (await SetAddress(second, name, "12 Main Road", "560001")).StatusCode);
    }

    // ----- asking

    [Fact]
    public async Task OnlyTheOwnerOfAShopCanAsk_ForAShopThatReallyHasThatNameAndAddress_AndOnlyOnce()
    {
        var name = "Ask Shop " + Tag();
        var old = await OwnerAsync($"ask-old-{Tag()}@example.com", name);
        await SetAddress(old, name, "5 Market Street");
        var fresh = await OwnerAsync($"ask-new-{Tag()}@example.com", name);
        var exec = await LoginAsync($"ask-exec-{Tag()}@example.com"); // does not exist: stays unauthenticated

        Assert.Equal(HttpStatusCode.NotFound, (await Ask(fresh, name, "7 Nowhere Lane")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Ask(fresh, name, "")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsJsonAsync("/api/shop-takeovers", new { name, address = "5 Market Street" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await exec.GetAsync("/api/shop-takeovers")).StatusCode);

        var asked = await Ask(fresh, name, "5, Market Street");
        Assert.Equal(HttpStatusCode.OK, asked.StatusCode);
        var dto = await Json(asked);
        Assert.Equal("Requested", dto.GetProperty("status").GetString());
        Assert.Equal("Outgoing", dto.GetProperty("direction").GetString());
        Assert.Contains("cancel", dto.GetProperty("can").EnumerateArray().Select(c => c.GetString()));
        Assert.DoesNotContain("ask-new", dto.GetProperty("requesterEmail").GetString()); // masked
        Assert.Equal(HttpStatusCode.Conflict, (await Ask(fresh, name, "5 Market Street")).StatusCode);

        // the old owner is told and sees it as incoming
        var told = await Json(await old.Client.GetAsync("/api/notifications"));
        Assert.Contains(told.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "TakeoverRequested" && n.GetProperty("linkUrl").GetString() == "/takeover");
        var incoming = (await Json(await old.Client.GetAsync("/api/shop-takeovers"))).EnumerateArray().Single();
        Assert.Equal("Incoming", incoming.GetProperty("direction").GetString());
        Assert.Equal(new[] { "confirm", "decline" }, incoming.GetProperty("can").EnumerateArray().Select(c => c.GetString()!));
    }

    // ----- only the owner swaps

    [Fact]
    public async Task WhenTheAssociatesStay_OnlyTheOwnerIsSwapped_AndTheNewOwnerMustConfirmWhoApproves()
    {
        var name = "Keep Shop " + Tag();
        var old = await OwnerAsync($"keep-old-{Tag()}@example.com", name);
        await SetAddress(old, name, "21 Lake Road");
        var execEmail = $"keep-exec-{Tag()}@example.com";
        await AddExecutiveAsync(old.ShopId, execEmail);
        var fresh = await OwnerAsync($"keep-new-{Tag()}@example.com", name);

        Assert.Equal(HttpStatusCode.BadRequest, (await SetAddress(fresh, name, "21 Lake Road")).StatusCode);
        var asked = await Json(await Ask(fresh, name, "21 Lake Road"));
        var id = asked.GetProperty("id").GetGuid();

        // the new owner cannot answer for the old one, the old owner needs the right password and a choice
        Assert.Equal(HttpStatusCode.Forbidden, (await fresh.Client.PostAsJsonAsync($"/api/shop-takeovers/{id}/confirm", new { associates = "Keep", password = "Password123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{id}/confirm", new { associates = "Keep", password = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{id}/confirm", new { password = "Password123!" })).StatusCode);

        var done = await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{id}/confirm", new { associates = "Keep", password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        var result = await Json(done);
        Assert.Equal("Completed", result.GetProperty("status").GetString());
        Assert.Equal("Keep", result.GetProperty("associates").GetString());
        Assert.False(result.GetProperty("decidedByAdmin").GetBoolean());

        // the old owner is out; the associate stays; the new owner is in the old shop
        Assert.Null((await LoginAsync(old.Email)).DefaultRequestHeaders.Authorization);
        Assert.NotNull((await LoginAsync(execEmail)).DefaultRequestHeaders.Authorization);
        var owner = await LoginAsync(fresh.Email);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(owner.DefaultRequestHeaders.Authorization!.Parameter!).Claims;
        Assert.Equal(old.ShopId.ToString(), claims.First(c => c.Type == "shop_id").Value);

        // approvals wait for the new owner to confirm who approves
        var team = await Json(await owner.GetAsync($"/api/shops/{old.ShopId}/team"));
        Assert.True(team.GetProperty("approvalReviewRequired").GetBoolean());

        var banner = (await Json(await owner.PostAsJsonAsync("/api/banners", new { name = "Sale", description = "", width = 100, height = 50 }))).GetProperty("id").GetGuid();
        var start = DateTime.UtcNow.AddDays(1);
        await owner.PutAsJsonAsync($"/api/banners/{banner}/schedule", new { startAt = start, endAt = start.AddDays(3) });
        var workflow = (await Json(await owner.PostAsJsonAsync("/api/publish-workflow/initiate", new { bannerId = banner }))).GetProperty("data").GetProperty("id").GetGuid();
        await owner.PostAsync($"/api/publish-workflow/{workflow}/submit", null);
        var blocked = await owner.PostAsJsonAsync($"/api/publish-workflow/{workflow}/approve", new { comment = "ok" });
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Contains("confirm who approves", (await Json(blocked)).GetProperty("message").GetString());

        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/shops/{old.ShopId}/team/confirm-approvers", null)).StatusCode);
        Assert.False((await Json(await owner.GetAsync($"/api/shops/{old.ShopId}/team"))).GetProperty("approvalReviewRequired").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/publish-workflow/{workflow}/approve", new { comment = "ok" })).StatusCode);

        // the new owner was told
        var inbox = await Json(await owner.GetAsync("/api/notifications"));
        Assert.Contains(inbox.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "TakeoverCompleted");
    }

    // ----- owner and associates change

    [Fact]
    public async Task WhenTheAssociatesChangeToo_TheOldShopCloses_AndTheNewShopTakesItsAddress()
    {
        var name = "Replace Shop " + Tag();
        var old = await OwnerAsync($"rep-old-{Tag()}@example.com", name);
        await SetAddress(old, name, "8 Hill Street");
        var execEmail = $"rep-exec-{Tag()}@example.com";
        await AddExecutiveAsync(old.ShopId, execEmail);
        var fresh = await OwnerAsync($"rep-new-{Tag()}@example.com", name);

        var id = (await Json(await Ask(fresh, name, "8 Hill Street"))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{id}/confirm", new { associates = "Replace", password = "Password123!" })).StatusCode);

        // nobody of the old shop can sign in any more
        Assert.Null((await LoginAsync(old.Email)).DefaultRequestHeaders.Authorization);
        Assert.Null((await LoginAsync(execEmail)).DefaultRequestHeaders.Authorization);

        // the new owner's shop has the address, and it is theirs to add associates to
        var owner = await LoginAsync(fresh.Email);
        var shop = await Json(await owner.GetAsync($"/api/shops/{fresh.ShopId}"));
        Assert.Equal("8 Hill Street", shop.GetProperty("address").GetString());
        Assert.False((await Json(await owner.GetAsync($"/api/shops/{fresh.ShopId}/team"))).GetProperty("approvalReviewRequired").GetBoolean());

        // the closed shop no longer counts as the same shop
        Assert.Equal(HttpStatusCode.OK, (await SetAddress(new Owner(owner, fresh.ShopId, fresh.UserId, fresh.Email), name, "8 Hill Street")).StatusCode);
    }

    // ----- declining, cancelling, administrators, rivals

    [Fact]
    public async Task TheOldOwnerCanDecline_TheAskerCanCancel_AndAnAnsweredRequestIsFinal()
    {
        var name = "Decline Shop " + Tag();
        var old = await OwnerAsync($"dec-old-{Tag()}@example.com", name);
        await SetAddress(old, name, "3 Park Lane");
        var fresh = await OwnerAsync($"dec-new-{Tag()}@example.com", name);

        var first = (await Json(await Ask(fresh, name, "3 Park Lane"))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await fresh.Client.PostAsJsonAsync($"/api/shop-takeovers/{first}/decline", new { })).StatusCode);
        var declined = await Json(await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{first}/decline", new { note = "I have not sold the shop" }));
        Assert.Equal("Declined", declined.GetProperty("status").GetString());
        Assert.Contains((await Json(await fresh.Client.GetAsync("/api/notifications"))).GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "TakeoverDeclined");
        Assert.Equal(HttpStatusCode.Conflict, (await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{first}/confirm", new { associates = "Keep", password = "Password123!" })).StatusCode);

        var second = (await Json(await Ask(fresh, name, "3 Park Lane"))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await old.Client.PostAsync($"/api/shop-takeovers/{second}/cancel", null)).StatusCode);
        Assert.Equal("Cancelled", (await Json(await fresh.Client.PostAsync($"/api/shop-takeovers/{second}/cancel", null))).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await fresh.Client.PostAsync($"/api/shop-takeovers/{second}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task AnAdministratorCanAnswerForAnOwnerWhoCannotBeReached_WithAReason_AndARivalRequestIsClosed()
    {
        var name = "Admin Shop " + Tag();
        var old = await OwnerAsync($"adm-old-{Tag()}@example.com", name);
        await SetAddress(old, name, "44 River Road");
        var winner = await OwnerAsync($"adm-win-{Tag()}@example.com", name);
        var rival = await OwnerAsync($"adm-rival-{Tag()}@example.com", name);
        var admin = await AdminAsync();

        var winning = (await Json(await Ask(winner, name, "44 River Road"))).GetProperty("id").GetGuid();
        await Ask(rival, name, "44 River Road");

        var listing = await admin.GetAsync("/api/shop-takeovers");
        Assert.Equal(HttpStatusCode.OK, listing.StatusCode);
        var seen = (await Json(listing)).EnumerateArray().Where(t => t.GetProperty("existingShopName").GetString() == name).ToList();
        Assert.Equal(2, seen.Count);
        Assert.All(seen, t => Assert.Contains("confirm-as-admin", t.GetProperty("can").EnumerateArray().Select(c => c.GetString())));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/shop-takeovers/{winning}/confirm-as-admin", new { associates = "Keep" })).StatusCode); // no reason
        Assert.Equal(HttpStatusCode.Forbidden, (await old.Client.PostAsJsonAsync($"/api/shop-takeovers/{winning}/confirm-as-admin", new { associates = "Keep", note = "x" })).StatusCode);

        var done = await Json(await admin.PostAsJsonAsync($"/api/shop-takeovers/{winning}/confirm-as-admin", new { associates = "Replace", note = "The owner sent the sale papers by post" }));
        Assert.Equal("Completed", done.GetProperty("status").GetString());
        Assert.True(done.GetProperty("decidedByAdmin").GetBoolean());

        // the other asker is told it went elsewhere
        var rivalSees = (await Json(await rival.Client.GetAsync("/api/shop-takeovers"))).EnumerateArray().Single();
        Assert.Equal("Declined", rivalSees.GetProperty("status").GetString());
        Assert.Contains("someone else", rivalSees.GetProperty("note").GetString());
    }
}
