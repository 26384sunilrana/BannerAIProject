using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class AdRateRulesTests
{
    private static AdRate Rate(AdRateLevel level, decimal price, ShopAdKind? kind = null, string? country = null, int? state = null, int? city = null, bool active = true) =>
        new() { Level = level, PricePerHour = price, Kind = kind, CountryCode = country, StateId = state, CityId = city, IsActive = active };

    private static AdRate? Pick(IEnumerable<AdRate> rates, ShopAdKind kind = ShopAdKind.Side) => AdRateRules.Pick(rates, "IN", 7, 70, kind);

    [Fact]
    public void TheNarrowestPlaceWins()
    {
        var rates = new[]
        {
            Rate(AdRateLevel.All, 10), Rate(AdRateLevel.Country, 20, country: "IN"), Rate(AdRateLevel.State, 30, state: 7), Rate(AdRateLevel.City, 40, city: 70),
        };

        Assert.Equal(40, Pick(rates)!.PricePerHour);
        Assert.Equal(30, Pick(rates.Take(3))!.PricePerHour);
        Assert.Equal(20, Pick(rates.Take(2))!.PricePerHour);
        Assert.Equal(10, Pick(rates.Take(1))!.PricePerHour);
    }

    [Fact]
    public void ARateForOtherPlacesDoesNotApply()
    {
        var rates = new[] { Rate(AdRateLevel.Country, 20, country: "US"), Rate(AdRateLevel.State, 30, state: 8), Rate(AdRateLevel.City, 40, city: 71) };

        Assert.Null(Pick(rates));
    }

    [Fact]
    public void WithinAPlaceTheRateForTheKindBeatsTheRateForAnyKind()
    {
        var rates = new[] { Rate(AdRateLevel.City, 40, city: 70), Rate(AdRateLevel.City, 90, ShopAdKind.Popup, city: 70) };

        Assert.Equal(90, Pick(rates, ShopAdKind.Popup)!.PricePerHour);
        Assert.Equal(40, Pick(rates, ShopAdKind.Side)!.PricePerHour);
    }

    [Fact]
    public void ANarrowerPlaceBeatsAWiderOneEvenWhenTheWiderIsForTheKind()
    {
        var rates = new[] { Rate(AdRateLevel.Country, 99, ShopAdKind.Side, country: "IN"), Rate(AdRateLevel.City, 40, city: 70) };

        Assert.Equal(40, Pick(rates)!.PricePerHour);
    }

    [Fact]
    public void SwitchedOffRatesAndRatesForAnotherKindAreIgnored()
    {
        var rates = new[] { Rate(AdRateLevel.City, 40, city: 70, active: false), Rate(AdRateLevel.All, 10, ShopAdKind.Mega) };

        Assert.Null(Pick(rates, ShopAdKind.Side));
    }

    [Fact]
    public void AShopWithNoCityOrStateOnlyGetsTheWiderRates()
    {
        var rates = new[] { Rate(AdRateLevel.City, 40, city: 70), Rate(AdRateLevel.All, 10) };

        Assert.Equal(10, AdRateRules.Pick(rates, null, null, null, ShopAdKind.Side)!.PricePerHour);
    }

    [Theory]
    [InlineData(ShopAdKind.Side, 20, 100, 20.0)]
    [InlineData(ShopAdKind.Mega, 60, 100, 60.0)]
    [InlineData(ShopAdKind.Side, 25, 33.33, 8.33)]
    [InlineData(ShopAdKind.Popup, 0, 100, 100.0)]
    [InlineData(ShopAdKind.Minor, 0, 40, 40.0)]
    public void SideAndMegaAdsAreCutDownToTheirShareOfTheScreen(ShopAdKind kind, int percent, double price, double expected)
    {
        var rate = Rate(AdRateLevel.All, (decimal)price);

        Assert.Equal((decimal)expected, AdRateRules.HourlyPrice(rate, new ShopAd { Kind = kind, SpacePercent = percent }));
    }

    [Fact]
    public void Validate_ChecksThePriceSharePlaceAndDecimals()
    {
        Assert.Null(AdRateRules.Validate(Rate(AdRateLevel.All, 0)));
        Assert.Null(AdRateRules.Validate(Rate(AdRateLevel.City, 120.5m, city: 1)));

        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.All, -1)));
        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.All, 1_000_001m)));
        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.All, 1.234m)));

        var share = Rate(AdRateLevel.All, 10);
        share.ShopSharePercent = 101;
        Assert.NotNull(AdRateRules.Validate(share));

        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.Country, 10)));
        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.State, 10)));
        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.City, 10)));
        Assert.NotNull(AdRateRules.Validate(Rate(AdRateLevel.All, 10, city: 3)));
    }
}
