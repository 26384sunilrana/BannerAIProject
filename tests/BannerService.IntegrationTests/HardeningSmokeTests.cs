using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>Stronger password hashes, security headers, audit export and encrypting stored values, through the real application.</summary>
public class HardeningSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public HardeningSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/authentication/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        return client;
    }

    [Fact]
    public async Task AnOldPasswordHash_StillSignsIn_AndIsReplacedByAStrongerOne()
    {
        await _factory.SeedAsync();
        var email = $"legacy-{Guid.NewGuid():N}@example.com";
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes("Password123!"), salt, 10_000, HashAlgorithmName.SHA256, 32);
        var legacy = Convert.ToBase64String(salt.Concat(key).ToArray());

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var shop = new Shop { Id = Guid.NewGuid(), Name = "Legacy Shop " + email, Status = ShopStatus.Active };
            context.Shops.Add(shop);
            var user = new User { Email = email, FirstName = "Old", LastName = "Hash", ShopId = shop.Id.ToString(), IsActive = true, PasswordHash = legacy };
            context.Users.Add(user);
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "2" });
            await context.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" })).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var hash = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AsNoTracking().FirstAsync(u => u.Email == email)).PasswordHash;
            Assert.StartsWith("v2$", hash);
        }

        // and it works again with the new hash, and a wrong password still fails
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" })).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await _factory.CreateClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Wrong-password1!" })).StatusCode);
    }

    [Fact]
    public async Task EveryAnswerCarriesTheSecurityHeaders_AndApiAnswersAreNotCached()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/locations/countries");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("no-store", response.Headers.GetValues("Cache-Control").Single());

        var admin = await LoginAsync("admin@example.com", "AdminPass123!");
        var data = await admin.GetAsync("/api/admin/users");
        Assert.Contains("no-store", data.Headers.GetValues("Cache-Control").Single());
    }

    [Fact]
    public async Task TheAuditLogCanBeExportedAsCsv_ByAdministratorsOnly_AndTheExportIsAudited()
    {
        var admin = await LoginAsync("admin@example.com", "AdminPass123!");
        var response = await admin.GetAsync("/api/admin/audit-logs/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Matches(@"^audit-log-\d{8}-\d{4}\.csv$", response.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        var csv = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("OccurredAtUtc,UserId,UserEmail,ShopId,Method,Path,StatusCode,IpAddress,DurationMs", csv);
        Assert.Contains("/api/authentication/login", csv); // the sign-in above is in the log

        // the export itself shows up in the next one
        var again = await (await admin.GetAsync("/api/admin/audit-logs/export")).Content.ReadAsStringAsync();
        Assert.Contains("/api/admin/audit-logs/export", again);

        var owner = _factory.CreateClient();
        var register = await owner.PostAsJsonAsync("/api/authentication/register", new { email = $"audit-{Guid.NewGuid():N}@example.com", password = "Password123!", firstName = "A", lastName = "B", shopName = "Audit Shop " + Guid.NewGuid().ToString("N")[..6] });
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(register)).GetProperty("tokens").GetProperty("accessToken").GetString()!);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/audit-logs/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/admin/audit-logs/export")).StatusCode);
    }

    [Fact]
    public async Task EncryptingStoredValues_NeedsFieldEncryptionSwitchedOn()
    {
        using var scope = _factory.Services.CreateScope();
        var backfill = new EncryptionBackfill(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

        // the test application runs with encryption off: the command refuses rather than doing nothing silently
        if (!FieldEncryption.IsConfigured)
            await Assert.ThrowsAsync<InvalidOperationException>(() => backfill.RunAsync());
    }
}
