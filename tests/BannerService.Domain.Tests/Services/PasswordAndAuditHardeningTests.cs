using System.Security.Cryptography;
using System.Text;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Application.Services;
using BannerService.Presentation.Controllers;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class PasswordAndAuditHardeningTests
{
    private readonly PasswordHashService _hasher = new();

    /// <summary>A hash exactly as the application made them before: plain base64 of salt and key, 10,000 rounds.</summary>
    private static string LegacyHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 10_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt.Concat(key).ToArray());
    }

    [Fact]
    public void NewHashesNameTheirStrength_AndVerify()
    {
        var hash = _hasher.HashPassword("Password123!");

        Assert.StartsWith($"v2${PasswordHashService.Iterations}$", hash);
        Assert.True(PasswordHashService.Iterations >= 300_000);
        Assert.True(_hasher.VerifyPassword("Password123!", hash));
        Assert.False(_hasher.VerifyPassword("password123!", hash));
        Assert.False(_hasher.NeedsRehash(hash));
        Assert.NotEqual(hash, _hasher.HashPassword("Password123!")); // a fresh salt each time
    }

    [Fact]
    public void OldHashesStillWork_AndAreMarkedForReplacement()
    {
        var old = LegacyHash("Password123!");

        Assert.True(_hasher.VerifyPassword("Password123!", old));
        Assert.False(_hasher.VerifyPassword("Wrong", old));
        Assert.True(_hasher.NeedsRehash(old));
    }

    [Fact]
    public void AHashWithFewerRoundsThanToday_NeedsReplacing_ButOneWithMoreDoesNot()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        string Make(int rounds) => $"v2${rounds}$" + Convert.ToBase64String(salt.Concat(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes("pw"), salt, rounds, HashAlgorithmName.SHA256, 32)).ToArray());

        Assert.True(_hasher.NeedsRehash(Make(100_000)));
        Assert.True(_hasher.VerifyPassword("pw", Make(100_000)));
        Assert.False(_hasher.NeedsRehash(Make(PasswordHashService.Iterations + 1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a hash")]
    [InlineData("v2$abc$AAAA")]
    [InlineData("v2$310000$")]
    [InlineData("v2$310000$AAAA")]
    [InlineData("v2$0$AAAA")]
    public void BrokenHashesNeverVerify_AndNeverThrow(string hash)
    {
        Assert.False(_hasher.VerifyPassword("Password123!", hash));
        Assert.False(_hasher.NeedsRehash(hash) && hash.Length == 0);
    }

    // ----- audit export

    [Fact]
    public void TheCsvHasAHeaderAndOneRowPerEntry()
    {
        var entries = new[]
        {
            new AuditLog { OccurredAt = new DateTime(2030, 1, 7, 10, 30, 0, DateTimeKind.Utc), UserId = "u1", UserEmail = "a@example.com", Method = "GET", Path = "/api/shops", StatusCode = 200, IpAddress = "10.0.0.1", DurationMs = 12 },
        };

        var lines = AuditCsv.Build(entries).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("OccurredAtUtc,UserId,UserEmail,ShopId,Method,Path,StatusCode,IpAddress,DurationMs", lines[0]);
        Assert.Equal("2030-01-07T10:30:00.000Z,u1,a@example.com,,GET,/api/shops,200,10.0.0.1,12", lines[1]);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]
    [InlineData("+1+1", "'+1+1")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("-5", "'-5")]
    [InlineData(null, "")]
    public void CellsAreQuotedAndFormulasDefused(string? value, string expected) => Assert.Equal(expected, AuditCsv.Cell(value));

    [Fact]
    public void ALineBreakInAPathStaysInsideItsCell()
    {
        var csv = AuditCsv.Build(new[] { new AuditLog { Method = "GET", Path = "/a\nb" } });

        Assert.Contains("\"/a\nb\"", csv);
    }

    // ----- logs

    [Theory]
    [InlineData("sam@example.com", "s***@example.com")]
    [InlineData("a@b.co", "a***@b.co")]
    [InlineData("nobody", "***")]
    [InlineData("@x.com", "***")]
    [InlineData("", "(none)")]
    [InlineData(null, "(none)")]
    public void LoggedAddressesKeepOnlyTheFirstLetterAndTheDomain(string? email, string expected) => Assert.Equal(expected, EmailService.Mask(email));
}
