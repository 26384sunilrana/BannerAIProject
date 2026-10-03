using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BannerService.IntegrationTests;

public class NotificationSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public NotificationSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid shopId)> OwnerAsync(string email)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Nora", lastName = "Notify", shopName = "Notify Shop " + email });
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var shopId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value);
        return (client, shopId);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Submitting_And_Approving_ABanner_LeaveMessages_ThatOnlyTheirOwnerCanReadAndClear()
    {
        var (owner, _) = await OwnerAsync("notify-owner@example.com");
        var (other, _) = await OwnerAsync("notify-other@example.com");

        Assert.Equal(0, (await Json(await owner.GetAsync("/api/notifications/unread-count"))).GetProperty("unread").GetInt32());

        var banner = (await Json(await owner.PostAsJsonAsync("/api/banners", new { name = "Sale", description = "", width = 100, height = 50 }))).GetProperty("id").GetGuid();
        var start = DateTime.UtcNow.AddDays(1);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/banners/{banner}/schedule", new { startAt = start, endAt = start.AddDays(3) })).StatusCode);
        var workflow = (await Json(await owner.PostAsJsonAsync("/api/publish-workflow/initiate", new { bannerId = banner }))).GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/publish-workflow/{workflow}/submit", null)).StatusCode);

        var inbox = await Json(await owner.GetAsync("/api/notifications"));
        Assert.Equal(1, inbox.GetProperty("unread").GetInt32());
        var first = inbox.GetProperty("items")[0];
        Assert.Equal("WorkflowSubmitted", first.GetProperty("kind").GetString());
        Assert.Equal("/approvals", first.GetProperty("linkUrl").GetString());
        Assert.Contains("Sale", first.GetProperty("message").GetString());
        Assert.False(first.GetProperty("isRead").GetBoolean());

        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/publish-workflow/{workflow}/approve", new { comment = "ok" })).StatusCode);
        var unread = await Json(await owner.GetAsync("/api/notifications?unreadOnly=true"));
        Assert.Equal(new[] { "WorkflowApproved", "WorkflowSubmitted" }, unread.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("kind").GetString()!).OrderBy(x => x));

        // someone else's messages are neither visible nor changeable
        var id = first.GetProperty("id").GetGuid();
        Assert.Equal(0, (await Json(await other.GetAsync("/api/notifications"))).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/notifications")).StatusCode);

        Assert.Equal(1, (await Json(await owner.PostAsync($"/api/notifications/{id}/read", null))).GetProperty("unread").GetInt32());
        var all = await Json(await owner.PostAsync("/api/notifications/read-all", null));
        Assert.Equal(0, all.GetProperty("unread").GetInt32());
        Assert.Equal(0, (await Json(await owner.GetAsync("/api/notifications/unread-count"))).GetProperty("unread").GetInt32());
        Assert.Equal(2, (await Json(await owner.GetAsync("/api/notifications"))).GetProperty("total").GetInt32());
    }
}
