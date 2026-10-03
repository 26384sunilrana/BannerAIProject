using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BannerService.Domain.Services;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace BannerService.IntegrationTests;

/// <summary>The application with two-step sign-in required for administrators.</summary>
public class TwoFactorRequiredFactory : SmokeFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Security:RequireTwoFactorForAdmins", "true");
    }
}

/// <summary>Two-step sign-in with an authenticator app, through the real application.</summary>
public class TwoFactorSmokeTests : IClassFixture<SmokeFactory>, IClassFixture<TwoFactorRequiredFactory>
{
    private readonly SmokeFactory _factory;
    private readonly TwoFactorRequiredFactory _required;

    public TwoFactorSmokeTests(SmokeFactory factory, TwoFactorRequiredFactory required)
    {
        _factory = factory;
        _required = required;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) throw new Exception($"{(int)response.StatusCode} {response.RequestMessage?.RequestUri?.AbsolutePath}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static void SignedInAs(HttpClient client, JsonElement login) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("tokens").GetProperty("accessToken").GetString()!);

    private static string CodeNow(string spacedSecret, int stepsAhead = 0) =>
        Totp.CodeFor(spacedSecret.Replace(" ", string.Empty), Totp.StepAt(DateTime.UtcNow) + stepsAhead);

    private async Task<(HttpClient client, string email)> OwnerAsync(SmokeFactory factory)
    {
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var email = $"twofa-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/authentication/register",
            new { email, password = "Password123!", firstName = "Tia", lastName = "Two", shopName = "Two Step Shop " + email });
        SignedInAs(client, await Json(register));
        return (client, email);
    }

    [Fact]
    public async Task SettingItUp_ThenSigningInNeedsTheCode_ARecoveryCodeWorksOnce_AndItCanBeSwitchedOff()
    {
        var (client, email) = await OwnerAsync(_factory);

        Assert.False((await Json(await client.GetAsync("/api/account/two-factor"))).GetProperty("enabled").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/two-factor/setup", new { password = "wrong" })).StatusCode);

        var setup = await Json(await client.PostAsJsonAsync("/api/account/two-factor/setup", new { password = "Password123!" }));
        var secret = setup.GetProperty("secret").GetString()!;
        Assert.StartsWith("otpauth://totp/BannerAI:", setup.GetProperty("uri").GetString());
        Assert.Contains(" ", secret);

        // nothing changes until the first code is confirmed
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/two-factor/enable", new { code = "000000" })).StatusCode);

        var enabled = await Json(await client.PostAsJsonAsync("/api/account/two-factor/enable", new { code = CodeNow(secret) }));
        var recovery = enabled.GetProperty("codes").EnumerateArray().Select(c => c.GetString()!).ToList();
        Assert.Equal(10, recovery.Count);
        var status = await Json(await client.GetAsync("/api/account/two-factor"));
        Assert.True(status.GetProperty("enabled").GetBoolean());
        Assert.Equal(10, status.GetProperty("recoveryCodesLeft").GetInt32());

        // the password alone no longer gives a session: only a challenge
        var anonymous = _factory.CreateClient();
        var first = await Json(await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }));
        Assert.True(first.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(first.TryGetProperty("tokens", out _));
        var challenge = first.GetProperty("challenge").GetString()!;

        // a challenge is not an access token
        var probe = _factory.CreateClient();
        probe.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", challenge);
        Assert.Equal(HttpStatusCode.Unauthorized, (await probe.GetAsync("/api/account")).StatusCode);

        // wrong code, junk challenge
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge, code = "123456" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge = "junk", code = CodeNow(secret, 1) })).StatusCode);

        // the right code finishes it (the step of the confirming code is spent, so the next one is used)
        var done = await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge, code = CodeNow(secret, 1) });
        var session = await Json(done);
        var signedIn = _factory.CreateClient();
        SignedInAs(signedIn, session);
        Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync("/api/account")).StatusCode);

        // the same code cannot be used again
        var again = await Json(await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge = again.GetProperty("challenge").GetString(), code = CodeNow(secret, 1) })).StatusCode);

        // a recovery code works once
        var third = await Json(await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }));
        var viaRecovery = await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge = third.GetProperty("challenge").GetString(), code = recovery[0].ToLowerInvariant() });
        Assert.Equal(HttpStatusCode.OK, viaRecovery.StatusCode);
        var fourth = await Json(await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge = fourth.GetProperty("challenge").GetString(), code = recovery[0] })).StatusCode);
        Assert.Equal(9, (await Json(await signedIn.GetAsync("/api/account/two-factor"))).GetProperty("recoveryCodesLeft").GetInt32());

        // switching it off needs the password and a code; then the password is enough again
        Assert.Equal(HttpStatusCode.BadRequest, (await signedIn.PostAsJsonAsync("/api/account/two-factor/disable", new { password = "Password123!", code = "000000" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await signedIn.PostAsJsonAsync("/api/account/two-factor/disable", new { password = "Password123!", code = CodeNow(secret, 1) })).StatusCode);
        Assert.False((await Json(await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }))).TryGetProperty("requiresTwoFactor", out _));
    }

    [Fact]
    public async Task WrongCodesCountTowardsTheLockout_ThenEvenTheRightCodeIsRefused()
    {
        var (client, email) = await OwnerAsync(_factory);
        var secret = (await Json(await client.PostAsJsonAsync("/api/account/two-factor/setup", new { password = "Password123!" }))).GetProperty("secret").GetString()!;
        await Json(await client.PostAsJsonAsync("/api/account/two-factor/enable", new { code = CodeNow(secret) }));

        var anonymous = _factory.CreateClient();
        var challenge = (await Json(await anonymous.PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }))).GetProperty("challenge").GetString()!;
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge, code = "000000" })).StatusCode);

        var locked = await anonymous.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge, code = CodeNow(secret, 1) });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Contains("locked", await locked.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NewRecoveryCodesReplaceTheOldOnes()
    {
        var (client, _) = await OwnerAsync(_factory);
        var secret = (await Json(await client.PostAsJsonAsync("/api/account/two-factor/setup", new { password = "Password123!" }))).GetProperty("secret").GetString()!;
        var old = (await Json(await client.PostAsJsonAsync("/api/account/two-factor/enable", new { code = CodeNow(secret) }))).GetProperty("codes");

        var fresh = await Json(await client.PostAsJsonAsync("/api/account/two-factor/recovery-codes", new { password = "Password123!", code = CodeNow(secret, 1) }));

        Assert.Equal(10, fresh.GetProperty("codes").GetArrayLength());
        Assert.NotEqual(old[0].GetString(), fresh.GetProperty("codes")[0].GetString());
    }

    [Fact]
    public async Task WhereRequired_AnAdministratorWithoutItCanOnlySetItUp_AndCannotSwitchItOff()
    {
        await _required.SeedAsync();
        var plain = _required.CreateClient();
        var login = await Json(await plain.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" }));
        SignedInAs(plain, login);

        // everything but the set-up screens is shut
        var shut = await plain.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, shut.StatusCode);
        Assert.Contains("two_factor_setup_required", await shut.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await plain.GetAsync("/api/shop-ads")).StatusCode);

        var status = await Json(await plain.GetAsync("/api/account/two-factor"));
        Assert.True(status.GetProperty("required").GetBoolean());

        var secret = (await Json(await plain.PostAsJsonAsync("/api/account/two-factor/setup", new { password = "AdminPass123!" }))).GetProperty("secret").GetString()!;
        await Json(await plain.PostAsJsonAsync("/api/account/two-factor/enable", new { code = CodeNow(secret) }));

        // the next sign-in asks for the code and then opens everything
        var fresh = _required.CreateClient();
        var challenge = (await Json(await fresh.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" }))).GetProperty("challenge").GetString()!;
        SignedInAs(fresh, await Json(await fresh.PostAsJsonAsync("/api/authentication/login/two-factor", new { challenge, code = CodeNow(secret, 1) })));
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/admin/users")).StatusCode);

        var off = await fresh.PostAsJsonAsync("/api/account/two-factor/disable", new { password = "AdminPass123!", code = CodeNow(secret, 2) });
        Assert.Equal(HttpStatusCode.Conflict, off.StatusCode);
        Assert.Contains("must use", await off.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnAdministratorCanSwitchItOffForSomeoneWhoLostTheirPhone()
    {
        var (client, email) = await OwnerAsync(_factory);
        var secret = (await Json(await client.PostAsJsonAsync("/api/account/two-factor/setup", new { password = "Password123!" }))).GetProperty("secret").GetString()!;
        await Json(await client.PostAsJsonAsync("/api/account/two-factor/enable", new { code = CodeNow(secret) }));
        var userId = new JwtSecurityTokenHandler().ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter!).Claims.First(c => c.Type.EndsWith("nameidentifier")).Value;

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/admin/users/{userId}/reset-two-factor", null)).StatusCode);

        var admin = _factory.CreateClient();
        SignedInAs(admin, await Json(await admin.PostAsJsonAsync("/api/authentication/login", new { email = "admin@example.com", password = "AdminPass123!" })));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/users/{userId}/reset-two-factor", null)).StatusCode);

        Assert.False((await Json(await _factory.CreateClient().PostAsJsonAsync("/api/authentication/login", new { email, password = "Password123!" }))).TryGetProperty("requiresTwoFactor", out _));
    }
}
