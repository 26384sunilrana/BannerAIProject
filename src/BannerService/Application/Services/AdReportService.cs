namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;

    public class AdUserReportRow
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public int Booked { get; set; }
        public int Approved { get; set; }
        public int SentBack { get; set; }
        public int Cancelled { get; set; }
        public int Waiting { get; set; }

        /// <summary>Ads whose text the screening sent to a compliance review.</summary>
        public int Flagged { get; set; }
    }

    public class AdUserReportDto
    {
        public string Month { get; set; } = string.Empty;
        public List<AdUserReportRow> Rows { get; set; } = new();
    }

    /// <summary>Who booked how many ads in a month, and how they fared. For administrators.</summary>
    public class AdReportService
    {
        private readonly IShopAdRepository _ads;
        private readonly IUserRepository _users;
        private readonly IShopRepository _shops;

        public AdReportService(IShopAdRepository ads, IUserRepository users, IShopRepository shops)
        {
            _ads = ads;
            _users = users;
            _shops = shops;
        }

        public async Task<AdUserReportDto> UsersAsync(string month)
        {
            if (!DateTime.TryParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var first))
                throw new ArgumentException("Choose a month, for example 2030-01.");

            var from = DateTime.SpecifyKind(first, DateTimeKind.Utc);
            var ads = await _ads.ListCreatedAsync(from, from.AddMonths(1));

            var rows = new List<AdUserReportRow>();
            foreach (var group in ads.GroupBy(a => a.CreatedByUserId))
            {
                var user = await _users.GetByIdAsync(group.Key.ToString());
                var shop = user != null && Guid.TryParse(user.ShopId, out var shopId) ? await _shops.GetByIdAsync(shopId) : null;
                var source = group.First().Source;
                rows.Add(new AdUserReportRow
                {
                    UserId = group.Key.ToString(),
                    Name = user?.GetFullName() is { Length: > 0 } name ? name : user?.Email ?? "(removed user)",
                    Role = source switch { ShopAdSource.Admin => "Administrator", ShopAdSource.ShopOwner => "Shop owner", _ => "Sales executive" },
                    ShopName = source == ShopAdSource.Admin ? string.Empty : shop?.Name ?? string.Empty,
                    Booked = group.Count(),
                    Approved = group.Count(a => a.Status is ShopAdStatus.Approved or ShopAdStatus.Overridden || (a.Status == ShopAdStatus.Cancelled && a.DecidedAt != null)),
                    SentBack = group.Count(a => a.Status == ShopAdStatus.Rejected),
                    Cancelled = group.Count(a => a.Status == ShopAdStatus.Cancelled),
                    Waiting = group.Count(a => a.Status is ShopAdStatus.PendingApproval or ShopAdStatus.PendingCompliance),
                    Flagged = group.Count(a => a.ComplianceNote != null),
                });
            }

            return new AdUserReportDto { Month = month, Rows = rows.OrderByDescending(r => r.Booked).ThenBy(r => r.Name).ToList() };
        }
    }
}
