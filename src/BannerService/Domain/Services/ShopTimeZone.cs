namespace BannerService.Domain.Services
{
    using Entities;

    /// <summary>Which time zone a shop works in, and where that came from.</summary>
    public sealed record ShopTimeZoneInfo(string Id, string Source, TimeZoneInfo Zone);

    public static class ShopTimeZone
    {
        public const string Fallback = "UTC";

        /// <summary>
        /// The shop's own setting, else its city's, else its country's, else UTC. A name the machine does not know is skipped
        /// (and the next one tried), so a bad value can never stop banners from being scheduled.
        /// Needs the shop loaded with its city and country.
        /// </summary>
        public static ShopTimeZoneInfo Resolve(Shop shop)
        {
            foreach (var (id, source) in new (string? id, string source)[]
                     {
                         (shop.TimeZoneId, "shop"),
                         (shop.CityNav?.TimeZoneId, "city"),
                         (shop.CountryNav?.TimeZoneId, "country"),
                     })
            {
                if (TimeZones.TryGet(id, out var zone))
                    return new ShopTimeZoneInfo(id!.Trim(), source, zone);
            }

            return new ShopTimeZoneInfo(Fallback, "default", TimeZoneInfo.Utc);
        }
    }
}
