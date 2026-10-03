using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class TotpTests
{
    // the secret of the RFC 6238 test vectors: ASCII "12345678901234567890"
    private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1111111111, "050471")]
    [InlineData(1234567890, "005924")]
    [InlineData(2000000000, "279037")]
    public void TheCodesOfTheStandardTestVectorsAreProduced(long seconds, string expected) =>
        Assert.Equal(expected, Totp.CodeFor(RfcSecret, seconds / 30));

    [Fact]
    public void ACodeIsAcceptedInItsOwnStepAndOneEitherSide_ButNotFurtherAway()
    {
        var now = new DateTime(2030, 1, 7, 10, 0, 10, DateTimeKind.Utc);
        var step = Totp.StepAt(now);

        Assert.True(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step), now, 0, out var matched));
        Assert.Equal(step, matched);
        Assert.True(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step - 1), now, 0, out _));
        Assert.True(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step + 1), now, 0, out _));
        Assert.False(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step - 2), now, 0, out _));
        Assert.False(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step + 2), now, 0, out _));
    }

    [Fact]
    public void ACodeCannotBeUsedTwice_NorAnOlderOne()
    {
        var now = new DateTime(2030, 1, 7, 10, 0, 10, DateTimeKind.Utc);
        var step = Totp.StepAt(now);
        Assert.True(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step), now, 0, out var used));

        Assert.False(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step), now, used, out _)); // the same code again
        Assert.False(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step - 1), now, used, out _)); // an older one
        Assert.True(Totp.Verify(RfcSecret, Totp.CodeFor(RfcSecret, step + 1), now, used, out _)); // the next one is fine
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public void MalformedCodesAreRefused(string? typed) =>
        Assert.False(Totp.Verify(RfcSecret, typed, DateTime.UtcNow, 0, out _));

    [Fact]
    public void SpacesAndDashesInATypedCodeAreIgnored()
    {
        var now = DateTime.UtcNow;
        var code = Totp.CodeFor(RfcSecret, Totp.StepAt(now));

        Assert.True(Totp.Verify(RfcSecret, code[..3] + " " + code[3..], now, 0, out _));
    }

    [Fact]
    public void SecretsAreRandom_Long_AndRoundTripThroughBase32()
    {
        var a = Totp.GenerateSecret();
        var b = Totp.GenerateSecret();

        Assert.NotEqual(a, b);
        Assert.Equal(32, a.Length);
        Assert.Matches("^[A-Z2-7]+$", a);
        Assert.Equal(a, Totp.Base32Encode(Totp.Base32Decode(a)));
        Assert.Equal(a, Totp.Base32Encode(Totp.Base32Decode(a.ToLowerInvariant().Insert(4, " ").Insert(9, "-"))));
        Assert.Throws<FormatException>(() => Totp.Base32Decode("not*base32"));
    }

    [Fact]
    public void TheAppAddressNamesTheIssuerTheAccountAndTheSecret()
    {
        var uri = Totp.Uri("BannerAI", "sam@example.com", "ABCDEFGH");

        Assert.StartsWith("otpauth://totp/BannerAI:sam%40example.com?secret=ABCDEFGH&issuer=BannerAI", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }

    [Fact]
    public void RecoveryCodesAreTenDifferentCodes_AndMatchWhateverWayTheyAreTyped()
    {
        var codes = Totp.GenerateRecoveryCodes();

        Assert.Equal(10, codes.Count);
        Assert.Equal(10, codes.Distinct().Count());
        Assert.All(codes, c => Assert.Matches("^[A-Z2-7]{5}-[A-Z2-7]{5}$", c));
        Assert.Equal(Totp.HashRecoveryCode(codes[0]), Totp.HashRecoveryCode(codes[0].ToLowerInvariant().Replace("-", " ")));
        Assert.NotEqual(Totp.HashRecoveryCode(codes[0]), Totp.HashRecoveryCode(codes[1]));
    }
}
