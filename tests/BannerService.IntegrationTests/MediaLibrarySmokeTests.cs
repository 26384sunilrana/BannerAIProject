using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>The media library through the real application: what is listed, what it adds up to, what is protected, what is cleaned up.</summary>
public class MediaLibrarySmokeTests : IClassFixture<SmokeFactory>
{
    private readonly SmokeFactory _factory;

    public MediaLibrarySmokeTests(SmokeFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid shopId)> RegisterOwnerAsync(string email)
    {
        await _factory.SeedAsync();
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Lib", lastName = "Owner", shopName = "Library Shop " + email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var shopId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "shop_id").Value);
        return (client, shopId);
    }

    private static string Md5(byte[] bytes) => Convert.ToHexString(System.Security.Cryptography.MD5.HashData(bytes)).ToLowerInvariant();

    private static byte[] PngWithSize(int width, int height, int length)
    {
        var bytes = new byte[length];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(bytes, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20), height);
        for (var i = 24; i < length; i++) bytes[i] = (byte)(i % 251);
        return bytes;
    }

    private static async Task<Guid> UploadPngAsync(HttpClient client, string name, byte[] bytes, bool finish = true)
    {
        var init = await client.PostAsJsonAsync("/api/media/upload/initialize",
            new { fileName = name, contentType = "image/png", totalSizeBytes = bytes.Length, fileType = 1 });
        var id = (await init.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mediaFileId").GetGuid();
        if (!finish) return id;

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{id}/chunks/0") { Content = new ByteArrayContent(bytes) };
        request.Headers.Add("X-Checksum-MD5", Md5(bytes));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/media/{id}/complete", null)).StatusCode);
        return id;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Library_ListsWithSizes_CountsStorage_ProtectsFilesInUse_AndCleansUp()
    {
        var (owner, shopId) = await RegisterOwnerAsync("library-owner@example.com");
        var (other, _) = await RegisterOwnerAsync("library-other@example.com");

        var hero = await UploadPngAsync(owner, "hero.png", PngWithSize(640, 480, 3000));
        var spare = await UploadPngAsync(owner, "spare-banner.png", PngWithSize(100, 50, 2000));
        var unfinished = await UploadPngAsync(owner, "never-finished.png", PngWithSize(10, 10, 500), finish: false);

        // the size is read from the file itself; the unfinished upload is not in the library
        var page = await Json(await owner.GetAsync("/api/media"));
        Assert.Equal(2, page.GetProperty("total").GetInt32());
        var heroItem = page.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == hero);
        Assert.Equal(640, heroItem.GetProperty("width").GetInt32());
        Assert.Equal(480, heroItem.GetProperty("height").GetInt32());
        Assert.False(heroItem.GetProperty("inUse").GetBoolean());
        Assert.True((await _factory.CreateClient().GetAsync(heroItem.GetProperty("url").GetString()!)).IsSuccessStatusCode);

        var byName = await Json(await owner.GetAsync("/api/media?search=SPARE"));
        Assert.Equal(spare, byName.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(0, (await Json(await owner.GetAsync("/api/media?type=video"))).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/api/media?type=sound")).StatusCode);

        var usage = await Json(await owner.GetAsync("/api/media/usage"));
        Assert.Equal(5000, usage.GetProperty("usedBytes").GetInt64());
        Assert.Equal(2, usage.GetProperty("fileCount").GetInt32());
        Assert.Equal(2, usage.GetProperty("imageCount").GetInt32());

        // another shop sees none of it and cannot delete it
        Assert.Equal(0, (await Json(await other.GetAsync("/api/media"))).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/media/{hero}")).StatusCode);

        // a banner uses the first file: it cannot be deleted, and the banner is named
        var banner = await Json(await owner.PostAsJsonAsync("/api/banners", new { name = "Summer Sale", description = "", width = 100, height = 50 }));
        var bannerId = banner.GetProperty("id").GetGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Components.Add(new Component(bannerId, ComponentType.Image, new Position(0, 0), new Size(10, 10), 1, "{\"mediaFileId\":\"" + hero + "\"}"));
            await context.SaveChangesAsync();
        }

        var refused = await owner.DeleteAsync($"/api/media/{hero}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        var refusedBody = await Json(refused);
        Assert.Contains("Summer Sale", refusedBody.GetProperty("message").GetString());
        Assert.Equal("Summer Sale", refusedBody.GetProperty("banners")[0].GetString());
        var listed = await Json(await owner.GetAsync("/api/media"));
        Assert.True(listed.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == hero).GetProperty("inUse").GetBoolean());

        // the unused one goes, and so do its bytes (its old link no longer works)
        var link = (await Json(await owner.GetAsync($"/api/media/{spare}/url"))).GetProperty("url").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync($"/api/media/{spare}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.CreateClient().GetAsync(link)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/api/media/{spare}")).StatusCode);
        Assert.Equal(1, (await Json(await owner.GetAsync("/api/media"))).GetProperty("total").GetInt32());
        Assert.Equal(3000, (await Json(await owner.GetAsync("/api/media/usage"))).GetProperty("usedBytes").GetInt64());

        // the clean-up removes the unfinished upload once it is a day old; an owner cannot run it
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsync("/api/media/admin/cleanup", null)).StatusCode);
        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" });
        var adminToken = (await Json(login)).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var early = await Json(await admin.PostAsync("/api/media/admin/cleanup", null));
        Assert.Equal(0, early.GetProperty("abandonedRemoved").GetInt32());

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var file = await context.MediaFiles.FirstAsync(m => m.Id == unfinished);
            file.CreatedAt = DateTime.UtcNow.AddHours(-30);
            await context.SaveChangesAsync();
        }
        var later = await Json(await admin.PostAsync("/api/media/admin/cleanup", null));
        Assert.Equal(1, later.GetProperty("abandonedRemoved").GetInt32());

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal((int)MediaFileStatus.Deleted, (await context.MediaFiles.FirstAsync(m => m.Id == unfinished)).Status);
            Assert.Equal(shopId, (await context.MediaFiles.FirstAsync(m => m.Id == hero)).ShopId);
            Assert.Equal((int)MediaFileStatus.Active, (await context.MediaFiles.FirstAsync(m => m.Id == hero)).Status);
        }
    }
}
