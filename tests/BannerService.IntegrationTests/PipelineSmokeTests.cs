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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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
        if (await context.Roles.AnyAsync())
            return;

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
}
