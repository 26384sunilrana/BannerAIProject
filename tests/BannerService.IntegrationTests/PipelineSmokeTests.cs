using BannerService.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>Starts the real application (real middleware, auth, filters and DI) on an in-memory database.</summary>
public class SmokeFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "Smoke_" + Guid.NewGuid();

    /// <summary>Set SMOKE_SQL to a SQL Server connection string to run the same tests against a real database (migrations and seeding included).</summary>
    private static readonly string? SqlConnection = Environment.GetEnvironmentVariable("SMOKE_SQL");

    private readonly string _mediaFolder = Path.Combine(Path.GetTempPath(), "banner-smoke-" + Guid.NewGuid());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("MediaService:LocalStoragePath", _mediaFolder);
        builder.UseSetting("Subscriptions:LifecycleEnabled", "false");   // the tests run the lifecycle themselves

        if (!string.IsNullOrEmpty(SqlConnection))
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", SqlConnection);
            return;
        }

        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(d =>
                         d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                         d.ServiceType == typeof(DbContextOptions) ||
                         d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true).ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.Users.AnyAsync(u => u.Email == "admin@example.com"))
            return;

        if (!await context.Roles.AnyAsync())
            context.Roles.AddRange(Role.DefaultRoles.Select(r => new Role { Id = r.Id, Name = r.Name, Description = r.Description }));

        var admin = new User
        {
            Email = "admin@example.com",
            PasswordHash = new PasswordHashService().HashPassword("AdminPass123!"),
            FirstName = "Ada",
            LastName = "Admin",
            ShopId = Guid.NewGuid().ToString()
        };
        context.Users.Add(admin);
        context.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = "1" });
        await context.SaveChangesAsync();
    }
}

public class PipelineSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public PipelineSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> TokenFrom(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }

    private async Task<(HttpClient client, string token, Guid shopId)> RegisterOwnerAsync(string email)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register", new
        {
            email,
            password = "Password123!",
            firstName = "Olive",
            lastName = "Owner",
            shopName = "Olive Mart"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await TokenFrom(response);
        var shopId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token, shopId);
    }

    [Fact]
    public async Task Health_IsPublic()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Is401()
    {
        await _factory.SeedAsync();

        var response = await _factory.CreateClient().GetAsync("/api/banners");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OwnerSignUp_ThenCallsOwnShopApis()
    {
        var (client, _, shopId) = await RegisterOwnerAsync("owner1@example.com");

        var banners = await client.GetAsync("/api/banners");
        var team = await client.GetAsync($"/api/shops/{shopId}/team");

        Assert.Equal(HttpStatusCode.OK, banners.StatusCode);
        Assert.Equal(HttpStatusCode.OK, team.StatusCode);
    }

    [Fact]
    public async Task Owner_CannotReachAnotherShopOrAdminApis()
    {
        var (client, _, _) = await RegisterOwnerAsync("owner2@example.com");

        var otherShop = await client.GetAsync($"/api/shops/{Guid.NewGuid()}/team");
        var otherDashboard = await client.GetAsync($"/api/shop/{Guid.NewGuid()}/dashboard/summary");
        var adminUsers = await client.GetAsync("/api/admin/users");
        var invoices = await client.GetAsync("/api/invoices/overdue");

        Assert.Equal(HttpStatusCode.Forbidden, otherShop.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherDashboard.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, adminUsers.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, invoices.StatusCode);
    }

    [Fact]
    public async Task SignUp_IntoAnExistingShop_IsRejected()
    {
        await _factory.SeedAsync();
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/authentication/register", new
        {
            email = "intruder@example.com",
            password = "Password123!",
            shopId = Guid.NewGuid().ToString(),
            shopName = "x"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanListUsers_AndSeesTheAuditTrail()
    {
        await RegisterOwnerAsync("owner3@example.com");
        var anonymous = _factory.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenFrom(login));

        var users = await anonymous.GetAsync("/api/admin/users?search=owner3");
        var audit = await anonymous.GetAsync("/api/admin/audit-logs?minStatusCode=0");

        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
        Assert.Contains("owner3@example.com", await users.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        var auditBody = await audit.Content.ReadAsStringAsync();
        Assert.Contains("/api/authentication/register", auditBody);
        Assert.Contains("/api/authentication/login", auditBody);
    }

    private static byte[] PngBytes(int length)
    {
        var bytes = new byte[length];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        for (var i = 8; i < length; i++) bytes[i] = (byte)(i % 251);
        return bytes;
    }

    private static string Md5(byte[] bytes) =>
        Convert.ToHexString(System.Security.Cryptography.MD5.HashData(bytes)).ToLowerInvariant();

    [Fact]
    public async Task Media_UploadInChunks_ThenDownloadWithSignedLink()
    {
        var (client, _, _) = await RegisterOwnerAsync("media1@example.com");
        var bytes = PngBytes(20_000);

        var init = await client.PostAsJsonAsync("/api/media/upload/initialize",
            new { fileName = "logo.png", contentType = "image/png", totalSizeBytes = bytes.Length, fileType = 1 });
        Assert.Equal(HttpStatusCode.OK, init.StatusCode);
        var initBody = await init.Content.ReadFromJsonAsync<JsonElement>();
        var mediaId = initBody.GetProperty("mediaFileId").GetGuid();
        Assert.Equal(1, initBody.GetProperty("totalChunks").GetInt32());

        // two pieces, sent out of order
        var pieces = new[] { bytes[..12_000], bytes[12_000..] };
        foreach (var number in new[] { 1, 0 })
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{mediaId}/chunks/{number}")
            {
                Content = new ByteArrayContent(pieces[number])
            };
            request.Headers.Add("X-Checksum-MD5", Md5(pieces[number]));
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        }

        var complete = await client.PostAsync($"/api/media/{mediaId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var link = (await (await client.GetAsync($"/api/media/{mediaId}/url?expirationMinutes=30")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("url").GetString()!;

        // the link works without any sign-in, which is how an <img> or <video> tag loads it
        var anonymous = _factory.CreateClient();
        var download = await anonymous.GetAsync(link);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync());

        // video players seek with range requests
        var ranged = new HttpRequestMessage(HttpMethod.Get, link);
        ranged.Headers.Range = new RangeHeaderValue(100, 199);
        var partial = await anonymous.SendAsync(ranged);
        Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode);
        Assert.Equal(bytes[100..200], await partial.Content.ReadAsByteArrayAsync());

        // an altered link is refused
        var tampered = await anonymous.GetAsync(link[..^2] + "AA");
        Assert.Equal(HttpStatusCode.Forbidden, tampered.StatusCode);
    }

    [Fact]
    public async Task Media_WrongChecksumHtmlContentAndOtherShops_AreRefused()
    {
        var (owner, _, _) = await RegisterOwnerAsync("media2@example.com");
        var (other, _, _) = await RegisterOwnerAsync("media3@example.com");

        var refused = await owner.PostAsJsonAsync("/api/media/upload/initialize",
            new { fileName = "x.svg", contentType = "image/svg+xml", totalSizeBytes = 100, fileType = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        var bytes = PngBytes(500);
        var init = await owner.PostAsJsonAsync("/api/media/upload/initialize",
            new { fileName = "a.png", contentType = "image/png", totalSizeBytes = bytes.Length, fileType = 1 });
        var mediaId = (await init.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mediaFileId").GetGuid();

        var bad = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{mediaId}/chunks/0") { Content = new ByteArrayContent(bytes) };
        bad.Headers.Add("X-Checksum-MD5", new string('0', 32));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.SendAsync(bad)).StatusCode);

        var good = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{mediaId}/chunks/0") { Content = new ByteArrayContent(bytes) };
        good.Headers.Add("X-Checksum-MD5", Md5(bytes));
        Assert.Equal(HttpStatusCode.OK, (await owner.SendAsync(good)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/media/{mediaId}/complete", null)).StatusCode);

        // another shop cannot get a link to, or look at, this file
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/media/{mediaId}/url")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/media/{mediaId}")).StatusCode);
    }

    [Fact]
    public async Task Dates_AreSentAsUtcWithAZone_AndTheScheduleRoundTrips()
    {
        var (client, _, _) = await RegisterOwnerAsync("dates@example.com");
        var created = await (await client.PostAsJsonAsync("/api/banners", new { name = "Dated", description = "", width = 100, height = 50 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var start = new DateTime(2030, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        var put = await client.PutAsJsonAsync($"/api/banners/{id}/schedule", new { startAt = start, endAt = start.AddHours(3) });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var json = await client.GetStringAsync($"/api/banners/{id}");
        var banner = JsonDocument.Parse(json).RootElement;

        Assert.EndsWith("Z", banner.GetProperty("publishStartAt").GetString());
        Assert.EndsWith("Z", banner.GetProperty("createdAt").GetString());
        Assert.Equal(start, banner.GetProperty("publishStartAt").GetDateTime().ToUniversalTime());

        // a window that overlaps is a conflict that names the other banner
        var other = await (await client.PostAsJsonAsync("/api/banners", new { name = "Other", description = "", width = 100, height = 50 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var clash = await client.PutAsJsonAsync($"/api/banners/{other.GetProperty("id").GetGuid()}/schedule",
            new { startAt = start.AddHours(1), endAt = start.AddHours(4) });
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        Assert.Contains("Dated", await clash.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EndedSubscription_SwitchesOffLogins_AndTheShopFallsBackToItsDefaultBanner()
    {
        var (client, _, shopId) = await RegisterOwnerAsync("lapsed@example.com");

        // subscribe, with auto renewal off and a renewal date that passed yesterday
        Guid subscriptionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Test", MonthlyPrice = 10, AnnualPrice = 100 };
            context.SubscriptionPlans.Add(plan);
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(), ShopId = shopId, PlanId = plan.Id, Status = SubscriptionStatus.Active,
                BillingPeriod = BillingPeriod.Monthly, AutoRenew = false, CurrentPrice = 10,
                RenewalDate = DateTime.UtcNow.AddDays(-1), StartDate = DateTime.UtcNow.AddMonths(-1)
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
            subscriptionId = subscription.Id;
        }

        async Task<JsonElement> Active() =>
            await (await client.GetAsync("/api/banners/active")).Content.ReadFromJsonAsync<JsonElement>();
        Task<HttpResponseMessage> Login() => _factory.CreateClient().PostAsJsonAsync("/api/authentication/login",
            new { email = "lapsed@example.com", password = "Password123!" });

        // the renewal date has passed and nobody renewed: a week of grace starts
        using (var scope = _factory.Services.CreateScope())
        {
            var report = await scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleService>().RunAsync(DateTime.UtcNow);
            Assert.Equal(1, report.EnteredGrace);
        }
        var grace = await Active();
        Assert.True(grace.GetProperty("useDefaultBanner").GetBoolean());
        Assert.Equal("SubscriptionEnded", grace.GetProperty("reason").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Login()).StatusCode);   // still able to sign in during grace

        // eight days later the week is over: logins are switched off
        using (var scope = _factory.Services.CreateScope())
        {
            var report = await scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleService>().RunAsync(DateTime.UtcNow.AddDays(8));
            Assert.Equal(1, report.Expired);
        }
        var refused = await Login();
        Assert.NotEqual(HttpStatusCode.OK, refused.StatusCode);
        Assert.Contains("subscription has ended", await refused.Content.ReadAsStringAsync());

        // the platform admin can reactivate, after which the owner can sign in again
        var admin = _factory.CreateClient();
        var adminLogin = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenFrom(adminLogin));
        var renew = await admin.PostAsync($"/api/subscriptions/{subscriptionId}/renew", null);
        Assert.True(renew.StatusCode == HttpStatusCode.OK, await renew.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await Login()).StatusCode);
    }

    [Fact]
    public async Task Admin_ListsEveryShopsSubscription_AndOwnersCannot()
    {
        var (owner, _, shopId) = await RegisterOwnerAsync("subs-owner@example.com");
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Listed", MonthlyPrice = 10, AnnualPrice = 100 };
            context.SubscriptionPlans.Add(plan);
            context.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(), ShopId = shopId, PlanId = plan.Id, Status = SubscriptionStatus.GracePeriod,
                BillingPeriod = BillingPeriod.Monthly, CurrentPrice = 10, RenewalDate = DateTime.UtcNow.AddDays(-2)
            });
            await context.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/subscriptions")).StatusCode);

        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenFrom(login));

        var all = await (await admin.GetAsync("/api/admin/subscriptions?search=Olive")).Content.ReadFromJsonAsync<JsonElement>();
        var grace = await (await admin.GetAsync($"/api/admin/subscriptions?status={(int)SubscriptionStatus.GracePeriod}")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, all.GetProperty("total").GetInt32());
        var row = all.GetProperty("items")[0];
        Assert.Equal("Olive Mart", row.GetProperty("shopName").GetString());
        Assert.Equal("Listed", row.GetProperty("planName").GetString());
        Assert.Equal((int)SubscriptionStatus.GracePeriod, row.GetProperty("status").GetInt32());
        Assert.EndsWith("Z", row.GetProperty("renewalDate").GetString());
        Assert.Equal(all.GetProperty("total").GetInt32(), grace.GetProperty("total").GetInt32());
    }
}
