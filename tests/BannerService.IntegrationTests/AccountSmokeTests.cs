using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>The cookie session, a person's own account, and handing over or moving logins - through the real application.</summary>
public class AccountSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public AccountSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private HttpClient RawClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    private static string? CookieHeader(HttpResponseMessage response, string name = "banner_refresh") =>
        response.Headers.TryGetValues("Set-Cookie", out var all) ? all.FirstOrDefault(c => c.StartsWith(name + "=")) : null;

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0].Split('=', 2)[1];

    private static async Task<string> AccessToken(HttpResponseMessage response) =>
        (await Json(response)).GetProperty("tokens").GetProperty("accessToken").GetString()!;

    private static HttpRequestMessage Post(string url, object? body, string? cookie = null, bool csrf = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body ?? new { }) };
        if (cookie != null) request.Headers.Add("Cookie", $"banner_refresh={cookie}");
        if (csrf) request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return request;
    }

    private async Task<(HttpClient client, string email, string accessToken, string cookie, Guid shopId, string userId)> RegisterAsync(string email, string shopName = "Account Shop")
    {
        await _factory.SeedAsync();
        var client = RawClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Olive", lastName = "Owner", shopName = shopName + " " + email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = CookieValue(CookieHeader(response)!);
        var token = await AccessToken(response);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, email, token, cookie, Guid.Parse(jwt.Claims.First(c => c.Type == "shop_id").Value),
            jwt.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value);
    }

    private async Task<string> AddExecutiveAsync(Guid shopId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Email = email, FirstName = "Sam", LastName = "Seller", ShopId = shopId.ToString(), IsActive = true,
            PasswordHash = new PasswordHashService().HashPassword("Password123!"),
        };
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "3" });
        await context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<HttpClient> AdminAsync()
    {
        await _factory.SeedAsync();
        var admin = RawClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AccessToken(login));
        return admin;
    }

    // ----- the cookie session

    [Fact]
    public async Task Login_PutsTheRefreshTokenInAnHttpOnlyCookie_AndLeavesItOutOfTheBody()
    {
        var (_, email, _, _, _, _) = await RegisterAsync("cookie1@example.com");

        var login = await RawClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" });

        var setCookie = CookieHeader(login)!;
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/authentication", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
        var tokens = (await Json(login)).GetProperty("tokens");
        Assert.Equal(string.Empty, tokens.GetProperty("refreshToken").GetString());
        Assert.False(string.IsNullOrEmpty(tokens.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Refresh_WithTheCookie_NeedsTheForgeryHeader_AndRotatesTheCookie()
    {
        var (_, _, _, cookie, _, _) = await RegisterAsync("cookie2@example.com");
        var client = RawClient();

        var forged = await client.SendAsync(Post("/api/authentication/refresh-token", new { }, cookie));
        Assert.Equal(HttpStatusCode.Forbidden, forged.StatusCode);

        var refreshed = await client.SendAsync(Post("/api/authentication/refresh-token", new { }, cookie, csrf: true));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var rotated = CookieValue(CookieHeader(refreshed)!);
        Assert.NotEqual(cookie, rotated);
        Assert.False(string.IsNullOrEmpty(await AccessToken(refreshed)));

        // the new cookie works
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Post("/api/authentication/refresh-token", new { }, rotated, csrf: true))).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithNoCookie_Is401()
    {
        await _factory.SeedAsync();

        var response = await RawClient().SendAsync(Post("/api/authentication/refresh-token", new { }, csrf: true));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TwoTabsRefreshingWithTheSameCookie_BothStaySignedIn()
    {
        var (_, _, _, cookie, _, _) = await RegisterAsync("cookie3@example.com");
        var client = RawClient();

        var first = await client.SendAsync(Post("/api/authentication/refresh-token", new { }, cookie, csrf: true));
        var second = await client.SendAsync(Post("/api/authentication/refresh-token", new { }, cookie, csrf: true)); // the same old cookie, a moment later

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(CookieValue(CookieHeader(first)!), CookieValue(CookieHeader(second)!)); // both end up holding the same new cookie
    }

    [Fact]
    public async Task Logout_EndsTheSession_AndClearsTheCookie_EvenWithAnExpiredAccessToken()
    {
        var (_, _, _, cookie, _, _) = await RegisterAsync("cookie4@example.com");
        var client = RawClient(); // no Authorization header at all: the access token has "expired"

        var forged = await client.SendAsync(Post("/api/authentication/logout", new { }, cookie));
        Assert.Equal(HttpStatusCode.Forbidden, forged.StatusCode);

        var logout = await client.SendAsync(Post("/api/authentication/logout", new { }, cookie, csrf: true));
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", CookieHeader(logout)!, StringComparison.OrdinalIgnoreCase);

        // the revoked token is useless, even straight away
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Post("/api/authentication/refresh-token", new { }, cookie, csrf: true))).StatusCode);
    }

    [Fact]
    public async Task ABrowserPageOnAnotherSite_IsNotAllowedToSendCredentials()
    {
        await _factory.SeedAsync();
        var client = RawClient();
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/authentication/refresh-token");
        preflight.Headers.Add("Origin", "https://evil.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "x-requested-with");

        var response = await client.SendAsync(preflight);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task TheLocalWebApp_IsAllowedToSendCredentials()
    {
        await _factory.SeedAsync();
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/authentication/refresh-token");
        preflight.Headers.Add("Origin", "http://localhost:3000");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "x-requested-with,content-type");

        var response = await RawClient().SendAsync(preflight);

        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    // ----- the person's own account

    [Fact]
    public async Task Account_ShowsAndUpdatesTheDetails()
    {
        var (client, email, _, _, shopId, _) = await RegisterAsync("account1@example.com");

        var account = await Json(await client.GetAsync("/api/account"));
        Assert.Equal(email, account.GetProperty("email").GetString());
        Assert.Equal(shopId.ToString(), account.GetProperty("shopId").GetString());
        Assert.Contains("ShopOwner", account.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));

        var updated = await client.PutAsJsonAsync("/api/account", new { firstName = "Olivia", lastName = "Owner", phoneNumber = "+91 98765 43210" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Olivia", (await Json(await client.GetAsync("/api/account"))).GetProperty("firstName").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/account", new { firstName = "", lastName = "x", phoneNumber = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RawClient().GetAsync("/api/account")).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_EndsOtherSessions_KeepsThisOne_AndTheNewPasswordWorks()
    {
        var (client, email, _, phoneCookie, _, _) = await RegisterAsync("account2@example.com");

        var wrong = await client.PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = "nope", newPassword = "BetterPass456!", confirmPassword = "BetterPass456!" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var changed = await client.PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = "Password123!", newPassword = "BetterPass456!", confirmPassword = "BetterPass456!" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.NotNull(CookieHeader(changed)); // a fresh session for this device
        Assert.Equal(string.Empty, (await Json(changed)).GetProperty("tokens").GetProperty("refreshToken").GetString());

        // the other device's session is gone, the old password no longer signs in, the new one does
        Assert.Equal(HttpStatusCode.Unauthorized, (await RawClient().SendAsync(Post("/api/authentication/refresh-token", new { }, phoneCookie, csrf: true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RawClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RawClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "BetterPass456!" })).StatusCode);
    }

    [Fact]
    public async Task SignOutEverywhere_EndsEverySession()
    {
        var (client, email, _, cookie, _, _) = await RegisterAsync("account3@example.com");
        var second = await RawClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" });
        var secondCookie = CookieValue(CookieHeader(second)!);

        var response = await client.PostAsync("/api/account/sign-out-everywhere", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await Json(response)).GetProperty("sessionsEnded").GetInt32() >= 2);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RawClient().SendAsync(Post("/api/authentication/refresh-token", new { }, cookie, csrf: true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RawClient().SendAsync(Post("/api/authentication/refresh-token", new { }, secondCookie, csrf: true))).StatusCode);
    }

    // ----- handing the shop over

    [Fact]
    public async Task Owner_HandsTheShopToAnExecutive_WithThePassword()
    {
        var (owner, ownerEmail, _, ownerCookie, shopId, ownerId) = await RegisterAsync("handover1@example.com");
        var samId = await AddExecutiveAsync(shopId, "sam-handover1@example.com");

        var wrong = await owner.PostAsJsonAsync($"/api/shops/{shopId}/team/transfer-ownership", new { newOwnerUserId = samId, password = "nope" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var done = await owner.PostAsJsonAsync($"/api/shops/{shopId}/team/transfer-ownership", new { newOwnerUserId = samId, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal(samId, (await Json(done)).GetProperty("newOwnerUserId").GetString());

        // roles swapped, the shop points at the new owner, the old owner's session is over
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(Guid.Parse(samId), (await context.Shops.FirstAsync(s => s.Id == shopId)).OwnerUserId);
            var roles = await context.UserRoles.Where(r => r.UserId == ownerId || r.UserId == samId).ToListAsync();
            Assert.Equal("3", roles.Single(r => r.UserId == ownerId).RoleId);
            Assert.Equal("2", roles.Single(r => r.UserId == samId).RoleId);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await RawClient().SendAsync(Post("/api/authentication/refresh-token", new { }, ownerCookie, csrf: true))).StatusCode);

        // the former owner cannot manage the team any more, even with the access token still valid for a few minutes
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/api/shops/{shopId}/team")).StatusCode);

        // the new owner signs in as owner
        var samLogin = await RawClient().PostAsJsonAsync("/api/authentication/login", new { email = "sam-handover1@example.com", password = "Password123!" });
        var roleClaims = new JwtSecurityTokenHandler().ReadJwtToken(await AccessToken(samLogin)).Claims.Where(c => c.Type.EndsWith("/role")).Select(c => c.Value);
        Assert.Contains("ShopOwner", roleClaims);
        Assert.DoesNotContain("SalesExecutive", roleClaims);
        _ = ownerEmail;
    }

    [Fact]
    public async Task Executive_CannotHandOverTheShop_AndOtherShopsOwnersCannotEither()
    {
        var (owner, _, _, _, shopId, _) = await RegisterAsync("handover2@example.com");
        var (intruder, _, _, _, _, _) = await RegisterAsync("handover3@example.com");
        var samId = await AddExecutiveAsync(shopId, "sam-handover2@example.com");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruder.PostAsJsonAsync($"/api/shops/{shopId}/team/transfer-ownership", new { newOwnerUserId = samId, password = "Password123!" })).StatusCode);

        var samClient = RawClient();
        var login = await samClient.PostAsJsonAsync("/api/authentication/login", new { email = "sam-handover2@example.com", password = "Password123!" });
        samClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AccessToken(login));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await samClient.PostAsJsonAsync($"/api/shops/{shopId}/team/transfer-ownership", new { newOwnerUserId = samId, password = "Password123!" })).StatusCode);
        _ = owner;
    }

    [Fact]
    public async Task Admin_AssignsAnOwner_AndMovesAnExecutiveToAnotherShop()
    {
        var (_, _, _, _, shopA, _) = await RegisterAsync("admin-tools-a@example.com");
        var (_, _, _, _, shopB, _) = await RegisterAsync("admin-tools-b@example.com");
        var samId = await AddExecutiveAsync(shopA, "sam-admin-tools@example.com");
        var admin = await AdminAsync();

        // an owner cannot use the administrator's tools
        var (plain, _, _, _, _, _) = await RegisterAsync("admin-tools-c@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await plain.PostAsJsonAsync($"/api/admin/users/{samId}/move", new { shopId = shopB })).StatusCode);

        // shop B has no subscription yet: not allowed
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/admin/users/{samId}/move", new { shopId = shopB })).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Moves", MonthlyPrice = 10, AnnualPrice = 100 };
            context.SubscriptionPlans.Add(plan);
            context.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(), ShopId = shopB, PlanId = plan.Id, Status = SubscriptionStatus.Active,
                BillingPeriod = BillingPeriod.Monthly, CurrentPrice = 10, RenewalDate = DateTime.UtcNow.AddDays(20)
            });
            await context.SaveChangesAsync();
        }

        var moved = await admin.PostAsJsonAsync($"/api/admin/users/{samId}/move", new { shopId = shopB });
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(shopB.ToString(), (await context.Users.FirstAsync(u => u.Id == samId)).ShopId);
        }

        // moving him again to his own shop is refused; the new shop's owner can be changed by the administrator
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/admin/users/{samId}/move", new { shopId = shopB })).StatusCode);
        var assigned = await admin.PostAsJsonAsync($"/api/admin/shops/{shopB}/owner", new { userId = samId });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/api/admin/shops/{shopB}/owner", new { userId = "missing" })).StatusCode);
    }
}
