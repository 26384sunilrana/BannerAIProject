namespace BannerService.Application.Services
{
    using System.Globalization;
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;

    public class AdStatementLineDto
    {
        public Guid AdId { get; set; }
        public string Headline { get; set; } = string.Empty;
        public string AdvertiserName { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>Hours the ad was on the screen in the month.</summary>
        public decimal Hours { get; set; }
        public decimal? PricePerHour { get; set; }
        public int? ShopSharePercent { get; set; }

        /// <summary>What the shop is paid for the month. Null for ads the shop booked itself: their price is the shop's own business.</summary>
        public decimal? Payout { get; set; }
    }

    public class ShopAdStatementDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string TimeZoneId { get; set; } = string.Empty;
        public List<AdStatementLineDto> Lines { get; set; } = new();
        public decimal AdminAdHours { get; set; }
        public decimal OwnAdHours { get; set; }
        public decimal Payout { get; set; }
    }

    public class AdStatementDto
    {
        public string Month { get; set; } = string.Empty;
        public List<ShopAdStatementDto> Shops { get; set; } = new();
        public decimal TotalPayout { get; set; }
    }

    /// <summary>
    /// The monthly statement of ads: for each shop, how many hours each ad ran in the month on the shop's clock and, for administrator ads,
    /// what the shop is paid for them. Ads the owner or executives booked are listed with their hours but carry no amount.
    /// </summary>
    public class AdStatementService
    {
        private readonly IShopAdRepository _ads;
        private readonly IShopRepository _shops;

        public AdStatementService(IShopAdRepository ads, IShopRepository shops)
        {
            _ads = ads;
            _shops = shops;
        }

        public async Task<AdStatementDto> GetAsync(AdActor actor, string month, Guid? requestedShopId, DateTime? nowUtc = null)
        {
            if (actor.Source == ShopAdSource.SalesExecutive) throw new UnauthorizedAccessException("Only the shop owner and the administrator can see the statement.");
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
                throw new ArgumentException("Choose a month, for example 2030-01.");

            Guid? shopId = requestedShopId;
            if (actor.Source != ShopAdSource.Admin)
            {
                if (requestedShopId != null && requestedShopId != actor.ShopId) throw new UnauthorizedAccessException("You can only see the statement of your own shop.");
                shopId = actor.ShopId;
            }

            var now = nowUtc ?? DateTime.UtcNow;

            // the month is wide enough on both sides for any time zone; each ad is then measured on its own shop's clock
            var ads = await _ads.ListApprovedAsync(shopId, DateTime.SpecifyKind(first, DateTimeKind.Utc).AddDays(-2), DateTime.SpecifyKind(first, DateTimeKind.Utc).AddMonths(1).AddDays(2));

            var result = new AdStatementDto { Month = month };
            foreach (var group in ads.GroupBy(a => a.ShopId))
            {
                var shop = await _shops.GetByIdAsync(group.Key);
                if (shop == null) continue;
                var zone = ShopTimeZone.Resolve(shop);
                var monthStart = LocalMidnightToUtc(first, zone.Zone);
                var monthEnd = LocalMidnightToUtc(first.AddMonths(1), zone.Zone);

                var statement = new ShopAdStatementDto { ShopId = shop.Id, ShopName = shop.Name, TimeZoneId = zone.Id };
                foreach (var ad in group.OrderBy(a => a.StartAt).ThenBy(a => a.Id))
                {
                    var hours = HoursRan(ad, zone.Zone, monthStart, monthEnd, now);
                    if (hours <= 0) continue;

                    decimal? payout = ad.Source == ShopAdSource.Admin && ad.PricePerHour != null
                        ? Math.Round(hours * ad.PricePerHour.Value * (ad.ShopSharePercent ?? 100) / 100m, 2, MidpointRounding.AwayFromZero)
                        : null;

                    statement.Lines.Add(new AdStatementLineDto
                    {
                        AdId = ad.Id, Headline = ad.Headline, AdvertiserName = ad.AdvertiserName, Kind = ad.Kind.ToString(), Source = ad.Source.ToString(),
                        Status = ad.Status.ToString(), Hours = hours, PricePerHour = ad.PricePerHour, ShopSharePercent = ad.ShopSharePercent, Payout = payout,
                    });
                }

                if (statement.Lines.Count == 0) continue;
                statement.AdminAdHours = statement.Lines.Where(l => l.Source == nameof(ShopAdSource.Admin)).Sum(l => l.Hours);
                statement.OwnAdHours = statement.Lines.Where(l => l.Source != nameof(ShopAdSource.Admin)).Sum(l => l.Hours);
                statement.Payout = statement.Lines.Sum(l => l.Payout ?? 0m);
                result.Shops.Add(statement);
            }

            result.Shops = result.Shops.OrderBy(s => s.ShopName).ToList();
            result.TotalPayout = result.Shops.Sum(s => s.Payout);
            return result;
        }

        /// <summary>
        /// Hours the ad was really on the screen inside the month: from when it was approved (never before), until it ended, was cancelled
        /// or overridden, or now. Daily hours and weekdays are respected on the shop's clock.
        /// </summary>
        public static decimal HoursRan(ShopAd ad, TimeZoneInfo zone, DateTime monthStart, DateTime monthEnd, DateTime now)
        {
            var from = Max(Max(monthStart, ad.StartAt), ad.DecidedAt ?? ad.StartAt);
            var to = Min(Min(monthEnd, ad.EndAt), Min(ad.StoppedAt ?? DateTime.MaxValue, now));
            if (to <= from) return 0m;

            var total = ScheduleOccurrences.For(ad.GetWindow(), ad.GetDailySchedule(), zone, from, to).Sum(o => (o.End - o.Start).TotalHours);
            return Math.Round((decimal)total, 2, MidpointRounding.AwayFromZero);
        }

        private static DateTime LocalMidnightToUtc(DateTime localDate, TimeZoneInfo zone)
        {
            var local = DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local)) local = local.AddHours(1);
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, zone), DateTimeKind.Utc);
        }

        private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;
    }
}
