using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class ShopIdentityTests
{
    [Theory]
    [InlineData("12, Main Road.", "12 main road")]
    [InlineData("  Olive   Mart ", "olive mart")]
    [InlineData("Café-Bar #5", "café bar 5")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("...", "")]
    public void NormaliseIgnoresCaseSpacesAndPunctuation(string? text, string expected) => Assert.Equal(expected, ShopIdentity.Normalise(text));

    [Fact]
    public void TheSameNameAtTheSameAddressIsTheSameShop_HoweverItIsWritten() =>
        Assert.True(ShopIdentity.Same("Olive Mart", "12, Main Road.", "400001", "OLIVE  MART", "12 main road", "400 001"));

    [Fact]
    public void ADifferentNameOrAddressIsAnotherShop()
    {
        Assert.False(ShopIdentity.Same("Olive Mart", "12 Main Road", null, "Olive Mart 2", "12 Main Road", null));
        Assert.False(ShopIdentity.Same("Olive Mart", "12 Main Road", null, "Olive Mart", "14 Main Road", null));
    }

    [Fact]
    public void PostalCodesCountOnlyWhenBothShopsHaveOne()
    {
        Assert.False(ShopIdentity.Same("A", "1 Road", "400001", "A", "1 Road", "560001"));
        Assert.True(ShopIdentity.Same("A", "1 Road", "400001", "A", "1 Road", null));
        Assert.True(ShopIdentity.Same("A", "1 Road", "", "A", "1 Road", "560001"));
    }

    [Theory]
    [InlineData(null, "1 Road")]
    [InlineData("1 Road", null)]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void AShopWithNoAddressYetMatchesNothing(string? a, string? b) => Assert.False(ShopIdentity.Same("A", a, null, "A", b, null));
}
