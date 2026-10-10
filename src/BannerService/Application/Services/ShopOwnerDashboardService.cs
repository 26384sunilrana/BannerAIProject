namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;

    public class ShopOwnerDashboardService : IShopOwnerDashboardService
    {
        private readonly IDashboardMetricsRepository _metricsRepository;
        private readonly IShopOwnerDashboardRepository _dashboardRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public ShopOwnerDashboardService(
            IDashboardMetricsRepository metricsRepository,
            IShopOwnerDashboardRepository dashboardRepository,
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _metricsRepository = metricsRepository;
            _dashboardRepository = dashboardRepository;
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
            _planRepository = planRepository;
        }

        public async Task<ShopDashboardMetrics> GetDashboardSummaryAsync(Guid shopId, bool forceRefresh = false)
        {
            var dashboard = await _dashboardRepository.GetCurrentAsync(shopId);

            if (dashboard != null && !forceRefresh && !dashboard.NeedsRefresh())
                return dashboard.CurrentSummary;

            var summary = new ShopDashboardMetrics
            {
                GeneratedAt = DateTime.UtcNow,
                SubscriptionMetrics = await GetSubscriptionMetricsAsync(shopId),
                RevenueMetrics = await GetRevenueMetricsAsync(shopId),
                BannerMetrics = await GetBannerMetricsAsync(shopId),
                UserMetrics = await GetUserMetricsAsync(shopId)
            };

            var dashboardEntity = new ShopOwnerDashboard
            {
                ShopId = shopId,
                CurrentSummary = summary
            };

            await _dashboardRepository.CreateOrUpdateAsync(dashboardEntity);
            return summary;
        }

        public async Task<ShopSubscriptionMetrics> GetSubscriptionMetricsAsync(Guid shopId)
        {
            var allSubscriptions = await _subscriptionRepository.GetAllAsync();
            var shopSubscriptions = allSubscriptions.Where(s => s.ShopId == shopId).ToList();
            var activeSubscriptions = shopSubscriptions.Where(s => s.Status == SubscriptionStatus.Active).ToList();
            var trialSubscriptions = shopSubscriptions.Where(s => s.Status == SubscriptionStatus.Trial).ToList();
            var paymentFailed = shopSubscriptions.Where(s => s.Status == SubscriptionStatus.PaymentFailed).ToList();
            var suspended = shopSubscriptions.Where(s => s.Status == SubscriptionStatus.Suspended).ToList();

            var expiringInDays = activeSubscriptions
                .Where(s => (s.RenewalDate - DateTime.UtcNow).TotalDays <= 7)
                .Count();

            var avgValue = activeSubscriptions.Any()
                ? activeSubscriptions.Average(s => s.CurrentPrice)
                : 0m;

            return new ShopSubscriptionMetrics
            {
                TotalSubscriptions = shopSubscriptions.Count,
                ActiveSubscriptions = activeSubscriptions.Count,
                TrialSubscriptions = trialSubscriptions.Count,
                ExpiringInDays = expiringInDays,
                PaymentFailedCount = paymentFailed.Count,
                SuspendedCount = suspended.Count,
                AverageSubscriptionValue = avgValue
            };
        }

        public async Task<ShopRevenueMetrics> GetRevenueMetricsAsync(Guid shopId)
        {
            var shopInvoices = await _invoiceRepository.GetByShopIdAsync(shopId);
            var paidInvoices = shopInvoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();

            var monthlyRevenue = paidInvoices
                .Where(i => i.PaidDate.HasValue &&
                           i.PaidDate.Value.AddMonths(1) > DateTime.UtcNow)
                .Sum(i => i.Amount);

            var annualRevenue = paidInvoices
                .Where(i => i.PaidDate.HasValue &&
                           i.PaidDate.Value.AddYears(1) > DateTime.UtcNow)
                .Sum(i => i.Amount);

            var activeSubscriptions = await _subscriptionRepository.GetActiveByShopIdAsync(shopId);

            var mrr = activeSubscriptions.Sum(s => s.CurrentPrice);
            var arr = mrr * 12;

            var draftInvoices = shopInvoices.Where(i => i.Status == InvoiceStatus.Draft).Count();
            var overdueInvoices = shopInvoices.Where(i => i.Status == InvoiceStatus.Overdue).Count();

            var collectionRate = paidInvoices.Any()
                ? (paidInvoices.Count / (decimal)shopInvoices.Count) * 100
                : 0m;

            var avgValue = activeSubscriptions.Any()
                ? activeSubscriptions.Average(s => s.CurrentPrice)
                : 0m;

            return new ShopRevenueMetrics
            {
                MonthlyCurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                MonthlyRevenue = monthlyRevenue,
                AnnualRevenue = annualRevenue,
                AverageRevenuePerSubscription = avgValue,
                CollectionRate = collectionRate,
                PendingPayments = draftInvoices,
                TotalOverdueAmount = overdueInvoices * 100m
            };
        }

        public async Task<ShopBannerMetrics> GetBannerMetricsAsync(Guid shopId)
        {
            var banners = await _metricsRepository.GetBannerCountsAsync(shopId);

            return new ShopBannerMetrics
            {
                TotalBanners = banners.Banners,
                PublishedBanners = banners.Published,
                DraftBanners = banners.Banners - banners.Published,
                BannersWithVersionControl = banners.WithVersions,
                TotalMediaStorageBytes = banners.MediaBytes,
                TotalComponents = banners.Components,
                TotalEffects = banners.Effects,
                TotalCarousels = banners.Carousels
            };
        }

        public async Task<ShopUserMetrics> GetUserMetricsAsync(Guid shopId)
        {
            var users = await _metricsRepository.GetUserCountsAsync(shopId);

            return new ShopUserMetrics
            {
                TotalUsers = users.Total,
                ActiveUsers = users.Active,
                InactiveUsers = users.Total - users.Active,
                UsersWithFailedLogin = users.WithFailedLogin,
                LockedOutUsers = users.LockedOut
            };
        }

        public async Task<List<ShopDashboardTrend>> GetTrendsAsync(Guid shopId, int days = 30)
        {
            var trends = new List<ShopDashboardTrend>();
            var snapshots = await _dashboardRepository.GetMetricSnapshotsAsync(shopId, "subscription_count", days);

            if (snapshots.Count >= 2)
            {
                var current = snapshots.Last().MetricValue;
                var previous = snapshots.First().MetricValue;
                var change = previous > 0 ? ((current - previous) / previous) * 100 : 0;

                trends.Add(new ShopDashboardTrend
                {
                    MetricName = "Total Subscriptions",
                    CurrentValue = current,
                    PreviousValue = previous,
                    PercentageChange = change,
                    Trend = change > 0 ? "UP" : (change < 0 ? "DOWN" : "STABLE"),
                    MeasuredAt = DateTime.UtcNow
                });
            }

            return trends;
        }

        public async Task<List<TopBannerByPerformance>> GetTopBannersAsync(Guid shopId, int limit = 10)
        {
            var banners = await _metricsRepository.GetRecentBannerSummariesAsync(shopId, limit);

            return banners.Select(b => new TopBannerByPerformance
            {
                BannerId = b.BannerId,
                BannerName = b.Name,
                ComponentCount = b.ComponentCount,
                EffectCount = b.EffectCount,
                CreatedAt = b.CreatedAt,
                PublishedAt = b.PublishedAt
            }).ToList();
        }

        public async Task<List<ShopDashboardAlert>> GetActiveAlertsAsync(Guid shopId)
        {
            return await _dashboardRepository.GetActiveAlertsAsync(shopId);
        }

        public async Task CreateShopAlertAsync(Guid shopId, string alertType, string message, ShopAlertSeverity severity)
        {
            var alert = new ShopDashboardAlert
            {
                ShopId = shopId,
                AlertType = alertType,
                Message = message,
                Severity = severity
            };

            await _dashboardRepository.CreateAlertAsync(alert);
        }
    }
}
