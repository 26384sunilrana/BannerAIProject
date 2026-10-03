using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>Effects, rotating pictures and rotating videos stored with a component, through the real application.</summary>
public class ComponentSettingsSmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public ComponentSettingsSmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid bannerId)> OwnerWithBannerAsync(string email)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Eve", lastName = "Effects", shopName = "Effects Shop " + email });
        var token = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var banner = await (await client.PostAsJsonAsync("/api/banners", new { name = "Hero", description = "", width = 1200, height = 600 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        return (client, banner.GetProperty("id").GetGuid());
    }

    private static object Component(int type, int z, object properties) =>
        new { componentType = type, positionX = 0, positionY = 0, sizeWidth = 400, sizeHeight = 200, zIndex = z, properties };

    private static async Task<JsonElement> PreviewComponent(HttpClient client, Guid bannerId, int index = 0)
    {
        var preview = await (await client.GetAsync($"/api/banners/{bannerId}/preview")).Content.ReadFromJsonAsync<JsonElement>();
        return preview.GetProperty("components")[index].GetProperty("properties");
    }

    [Fact]
    public async Task AnEffectAndRotatingPictures_AreStoredWithTheComponent_AndComeBackInThePreview()
    {
        var (client, bannerId) = await OwnerWithBannerAsync("fx1@example.com");

        var response = await client.PostAsJsonAsync($"/api/banners/{bannerId}/components", Component(2, 0, new
        {
            mediaFileId = "main",
            slides = new[] { new { mediaFileId = "a", alt = "A" }, new { mediaFileId = "b", alt = "B" } },
            slideIntervalSeconds = 4,
            slideTransition = "slideLeft",
            slideTransitionMs = 600,
            effect = new { kind = "zoomIn", durationMs = 900, delayMs = 250 },
        }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var props = await PreviewComponent(client, bannerId);
        Assert.Equal(2, props.GetProperty("slides").GetArrayLength());
        Assert.Equal("b", props.GetProperty("slides")[1].GetProperty("mediaFileId").GetString());
        Assert.Equal("slideLeft", props.GetProperty("slideTransition").GetString());
        Assert.Equal("zoomIn", props.GetProperty("effect").GetProperty("kind").GetString());
        Assert.Equal(250, props.GetProperty("effect").GetProperty("delayMs").GetInt32());
    }

    [Theory]
    [InlineData("{\"effect\":{\"kind\":\"explode\",\"durationMs\":800,\"delayMs\":0}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":1,\"delayMs\":0}}")]
    [InlineData("{\"slides\":[{\"alt\":\"no file\"}]}")]
    [InlineData("{\"slides\":[{\"mediaFileId\":\"a\"}],\"slideIntervalSeconds\":999}")]
    [InlineData("{\"slides\":[{\"mediaFileId\":\"a\"}],\"slideTransition\":\"spin\"}")]
    public async Task BadSettings_AreRefusedWithAMessage_AndNothingIsStored(string json)
    {
        var (client, bannerId) = await OwnerWithBannerAsync($"fx-bad-{Math.Abs(json.GetHashCode())}@example.com");
        var properties = JsonSerializer.Deserialize<JsonElement>(json);

        var response = await client.PostAsJsonAsync($"/api/banners/{bannerId}/components", Component(2, 0, properties));

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity, $"was {response.StatusCode}");
        var preview = await (await client.GetAsync($"/api/banners/{bannerId}/preview")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, preview.GetProperty("components").GetArrayLength());
    }

    [Fact]
    public async Task AVideoPlaylistOfUploadedFiles_IsAccepted_AndTheServerWorksOutThePlan()
    {
        var (client, bannerId) = await OwnerWithBannerAsync("fx2@example.com");

        var response = await client.PostAsJsonAsync($"/api/banners/{bannerId}/components", Component(3, 0, new
        {
            playlist = new object[] { new { mediaFileId = "v1", durationSeconds = 4.0 }, new { mediaFileId = "v2", durationSeconds = 60.0 } },
            rotationMode = "fixedSeconds",
            secondsPerVideo = 10,
            muted = false,
            volume = 0.5,
            loop = true,
        }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var props = await PreviewComponent(client, bannerId);
        var steps = props.GetProperty("playbackPlan").GetProperty("steps");
        Assert.Equal(4, steps[0].GetProperty("playSeconds").GetDouble()); // shorter than the 10 seconds: it ends first
        Assert.Equal(10, steps[1].GetProperty("playSeconds").GetDouble());
        Assert.Equal("v2", steps[1].GetProperty("mediaFileId").GetString());
        Assert.Equal(0.5, props.GetProperty("playbackPlan").GetProperty("volume").GetDouble());
    }

    [Fact]
    public async Task AVideoPlaylistNeedsAFileOrALinkForEveryVideo()
    {
        var (client, bannerId) = await OwnerWithBannerAsync("fx3@example.com");

        var response = await client.PostAsJsonAsync($"/api/banners/{bannerId}/components", Component(3, 0, new
        {
            playlist = new object[] { new { durationSeconds = 4.0 } },
        }));

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity, $"was {response.StatusCode}");
    }

    [Fact]
    public async Task Updating_AComponent_KeepsAndChecksTheSettingsToo()
    {
        var (client, bannerId) = await OwnerWithBannerAsync("fx4@example.com");
        var created = await (await client.PostAsJsonAsync($"/api/banners/{bannerId}/components", Component(2, 0, new { mediaFileId = "m" })))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var good = await client.PutAsJsonAsync($"/api/banners/{bannerId}/components/{id}",
            Component(2, 0, new { mediaFileId = "m", effect = new { kind = "pulse", durationMs = 1200, delayMs = 0 } }));
        var bad = await client.PutAsJsonAsync($"/api/banners/{bannerId}/components/{id}",
            Component(2, 0, new { mediaFileId = "m", effect = new { kind = "pulse", durationMs = 1, delayMs = 0 } }));

        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.True(bad.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity);
        Assert.Equal("pulse", (await PreviewComponent(client, bannerId)).GetProperty("effect").GetProperty("kind").GetString());
    }
}
