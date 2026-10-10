namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class DashboardMetricsRepository : IDashboardMetricsRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardMetricsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShopCounts> GetShopCountsAsync()
        {
            var shops = _context.Set<Shop>().AsNoTracking();

            return new ShopCounts
            {
                Total = await shops.CountAsync(),
                TopLevel = await shops.CountAsync(s => s.ParentShopId == null),
                Active = await shops.CountAsync(s => s.Status == ShopStatus.Active),
                WithActiveSubscription = await _context.Set<Subscription>()
                    .AsNoTracking()
                    .Where(s => s.Status == SubscriptionStatus.Active)
                    .Select(s => s.ShopId)
                    .Distinct()
                    .CountAsync()
            };
        }

        public Task<int> GetUserTotalCountAsync()
        {
            return _context.Set<User>().AsNoTracking().CountAsync();
        }

        public async Task<UserCounts> GetUserCountsAsync(Guid? shopId = null)
        {
            var users = _context.Set<User>().AsNoTracking();
            if (shopId.HasValue)
            {
                var shop = shopId.Value.ToString();
                users = users.Where(u => u.ShopId == shop);
            }

            var counts = new UserCounts
            {
                Total = await users.CountAsync(),
                Active = await users.CountAsync(u => u.IsActive),
                WithFailedLogin = await users.CountAsync(u => u.LoginAttempts > 0),
                LockedOut = await users.CountAsync(u => u.IsLockedOut)
            };

            if (!shopId.HasValue)
            {
                var userRoles = _context.Set<UserRole>().AsNoTracking();
                counts.AdminRole = await userRoles.CountAsync(ur => ur.RoleId == "1");
                counts.ShopOwnerRole = await userRoles.CountAsync(ur => ur.RoleId == "2");
                counts.SalesExecutiveRole = await userRoles.CountAsync(ur => ur.RoleId == "3");
            }

            return counts;
        }

        public async Task<BannerCounts> GetBannerCountsAsync(Guid? shopId = null)
        {
            var banners = _context.Set<Banner>().AsNoTracking();
            if (shopId.HasValue)
                banners = banners.Where(b => b.ShopId == shopId.Value);

            var bannerIds = banners.Select(b => b.Id);
            var components = _context.Set<Component>().AsNoTracking().Where(c => bannerIds.Contains(c.BannerId));
            var componentIds = components.Select(c => c.Id);

            var media = _context.Set<MediaFile>().AsNoTracking();
            if (shopId.HasValue)
                media = media.Where(m => m.ShopId == shopId.Value);

            return new BannerCounts
            {
                Banners = await banners.CountAsync(),
                Published = await banners.CountAsync(b => b.IsPublished),
                WithVersions = await _context.Set<BannerVersion>().AsNoTracking()
                    .Where(v => bannerIds.Contains(v.BannerId))
                    .Select(v => v.BannerId)
                    .Distinct()
                    .CountAsync(),
                Components = await components.CountAsync(),
                Effects = await _context.Set<Effect>().AsNoTracking()
                    .CountAsync(e => componentIds.Contains(e.ComponentId)),
                Carousels = await _context.Set<Carousel>().AsNoTracking()
                    .CountAsync(c => bannerIds.Contains(c.BannerId)),
                MediaBytes = await media.SumAsync(m => (long?)m.SizeBytes) ?? 0L
            };
        }

        public async Task<List<BannerSummary>> GetRecentBannerSummariesAsync(Guid shopId, int limit)
        {
            var banners = await _context.Set<Banner>().AsNoTracking()
                .Where(b => b.ShopId == shopId)
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var ids = banners.Select(b => b.Id).ToList();

            var componentCounts = await _context.Set<Component>().AsNoTracking()
                .Where(c => ids.Contains(c.BannerId))
                .GroupBy(c => c.BannerId)
                .Select(g => new { BannerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BannerId, x => x.Count);

            var effectCounts = await (
                    from e in _context.Set<Effect>().AsNoTracking()
                    join c in _context.Set<Component>().AsNoTracking() on e.ComponentId equals c.Id
                    where ids.Contains(c.BannerId)
                    group e by c.BannerId into g
                    select new { BannerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BannerId, x => x.Count);

            return banners.Select(b => new BannerSummary
            {
                BannerId = b.Id,
                Name = b.Name,
                CreatedAt = b.CreatedAt,
                PublishedAt = b.IsPublished ? b.UpdatedAt : null,
                ComponentCount = componentCounts.GetValueOrDefault(b.Id),
                EffectCount = effectCounts.GetValueOrDefault(b.Id)
            }).ToList();
        }

        public Task<List<SubscriptionPlan>> GetAllPlansAsync()
        {
            return _context.Set<SubscriptionPlan>().AsNoTracking().ToListAsync();
        }

        public async Task<PaymentCounts> GetPaymentCountsAsync()
        {
            var invoices = _context.Set<Invoice>().AsNoTracking();

            return new PaymentCounts
            {
                Total = await invoices.CountAsync(),
                Paid = await invoices.CountAsync(i => i.Status == InvoiceStatus.Paid),
                Overdue = await invoices.CountAsync(i => i.Status == InvoiceStatus.Overdue),
                Refunded = await invoices.CountAsync(i => i.Status == InvoiceStatus.Refunded),
                AverageAmount = await invoices.AverageAsync(i => (decimal?)i.Amount) ?? 0m,
                LastPaidDate = await invoices.MaxAsync(i => i.PaidDate),
                SubscriptionsInGracePeriod = await _context.Set<Subscription>().AsNoTracking()
                    .CountAsync(s => s.Status == SubscriptionStatus.GracePeriod)
            };
        }

        public Task<int> GetSubscriptionCountAsync()
        {
            return _context.Set<Subscription>().AsNoTracking().CountAsync();
        }

        public Task<int> GetInvoiceCountAsync()
        {
            return _context.Set<Invoice>().AsNoTracking().CountAsync();
        }
    }
}
