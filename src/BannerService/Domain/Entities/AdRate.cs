namespace BannerService.Domain.Entities;

/// <summary>How wide a rate reaches. A narrower rate wins over a wider one.</summary>
public enum AdRateLevel
{
    /// <summary>Every shop.</summary>
    All = 0,
    Country = 1,
    State = 2,
    City = 3,
}

/// <summary>
/// What the administrator charges for an hour of an ad on a shop screen in a place, and how much of it the shop is paid. For side and
/// mega ads the price is for the whole screen and is cut down to the share the ad takes; popups and corner tiles are a flat price per hour.
/// </summary>
public class AdRate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AdRateLevel Level { get; set; }
    public string? CountryCode { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }

    /// <summary>Null means the rate is for every kind of ad.</summary>
    public ShopAdKind? Kind { get; set; }

    public decimal PricePerHour { get; set; }

    /// <summary>The part of the price the shop is paid, in percent.</summary>
    public int ShopSharePercent { get; set; } = 100;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
