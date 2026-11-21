using BannerService.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class PlatformCurrencyTests
{
    private static PlatformCurrency From(string? value)
    {
        var settings = new Dictionary<string, string?>();
        if (value != null) settings["Payments:DefaultCurrency"] = value;
        return new PlatformCurrency(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    [Theory]
    [InlineData(null, "INR")]
    [InlineData("", "INR")]
    [InlineData("usd", "USD")]
    [InlineData(" EUR ", "EUR")]
    [InlineData("RUPEES", "INR")]
    [InlineData("12", "INR")]
    public void TheCurrencyIsTheSetting_InrWhenItIsMissingOrNotAThreeLetterCode(string? setting, string expected) =>
        Assert.Equal(expected, From(setting).Code);

    [Fact]
    public void WithoutTheSetting_TheDefaultIsInr() => Assert.Equal("INR", PlatformCurrency.Default.Code);
}
