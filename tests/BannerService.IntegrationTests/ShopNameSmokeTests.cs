using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BannerService.IntegrationTests;

public class ShopNameSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public ShopNameSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid shopId)> OwnerAsync(string email, string shopName)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Sam", lastName = "Same", shopName });
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var shopId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value);
        return (client, shopId);
    }

    [Fact]
    public async Task AShopSharingItsNameWithAnother_CanStillSaveOtherDetails_ButCannotTakeANameInUse()
    {
        var name = "Same Name Shop " + Guid.NewGuid().ToString("N")[..8];
        var (first, firstId) = await OwnerAsync("same-name-1-" + Guid.NewGuid().ToString("N")[..6] + "@example.com", name);
        await OwnerAsync("same-name-2-" + Guid.NewGuid().ToString("N")[..6] + "@example.com", name);

        // sign-up allowed both; editing the second details of one of them must not fail on its own name
        var kept = await first.PutAsJsonAsync($"/api/shops/{firstId}", new { name, description = "Open late", status = 1 });
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);

        // taking another shop's name by renaming is still refused
        var (third, thirdId) = await OwnerAsync("same-name-3-" + Guid.NewGuid().ToString("N")[..6] + "@example.com", "Different " + name);
        var taken = await third.PutAsJsonAsync($"/api/shops/{thirdId}", new { name, status = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
    }
}
