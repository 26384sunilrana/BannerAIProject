namespace BannerService.Domain.Services;

using Entities;

public static class AdRateRules
{
    public const decimal MaxPricePerHour = 1_000_000m;

    /// <summary>
    /// Picks the rate for an ad on a shop: the narrowest place first (city, state, country, everywhere), and within a place a rate for
    /// the kind of ad before a rate for every kind. Returns null when none applies.
    /// </summary>
    public static AdRate? Pick(IEnumerable<AdRate> rates, string? countryCode, int? stateId, int? cityId, ShopAdKind kind) =>
        rates
            .Where(r => r.IsActive && (r.Kind == null || r.Kind == kind))
            .Where(r => r.Level switch
            {
                AdRateLevel.All => true,
                AdRateLevel.Country => countryCode != null && string.Equals(r.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase),
                AdRateLevel.State => stateId != null && r.StateId == stateId,
                AdRateLevel.City => cityId != null && r.CityId == cityId,
                _ => false,
            })
            .OrderByDescending(r => (int)r.Level)
            .ThenByDescending(r => r.Kind != null)
            .FirstOrDefault();

    /// <summary>What an hour of the ad costs: the rate cut down to the share of the screen for side and mega ads.</summary>
    public static decimal HourlyPrice(AdRate rate, ShopAd ad) =>
        ad.Kind is ShopAdKind.Side or ShopAdKind.Mega
            ? Math.Round(rate.PricePerHour * ad.SpacePercent / 100m, 2, MidpointRounding.AwayFromZero)
            : rate.PricePerHour;

    public static string? Validate(AdRate rate)
    {
        if (rate.PricePerHour < 0 || rate.PricePerHour > MaxPricePerHour) return $"The price per hour must be between 0 and {MaxPricePerHour:0}.";
        if (rate.PricePerHour != Math.Round(rate.PricePerHour, 2)) return "The price can have at most two decimals.";
        if (rate.ShopSharePercent < 0 || rate.ShopSharePercent > 100) return "The shop share must be between 0 and 100 percent.";
        switch (rate.Level)
        {
            case AdRateLevel.All:
                if (rate.CountryCode != null || rate.StateId != null || rate.CityId != null) return "A rate for every shop has no place.";
                break;
            case AdRateLevel.Country:
                if (string.IsNullOrWhiteSpace(rate.CountryCode)) return "Choose the country.";
                break;
            case AdRateLevel.State:
                if (rate.StateId == null) return "Choose the state.";
                break;
            case AdRateLevel.City:
                if (rate.CityId == null) return "Choose the city.";
                break;
            default:
                return "Choose where the rate applies.";
        }
        return null;
    }
}
