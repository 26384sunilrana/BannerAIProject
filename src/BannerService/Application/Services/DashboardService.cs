namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;

    public class DashboardService : IDashboardService
    {
        private readonly IDashboardMetricsRepository _metricsRepository;
        private readonly IAdminDashboardRepository _dashboardRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public DashboardService(
            IDashboardMetricsRepository metricsRepository,
            IAdminDashboardRepository dashboardRepository,
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            IShopRepository shopRepository,
            IUserRepository userRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _metricsRepository = metricsRepository;
            _dashboardRepository = dashboardRepository;
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
            _shopRepository = shopRepository;
            _userRepository = userRepository;
            _planRepository = planRepository;
        }

        public async Task<DashboardSummary> GetDashboardSummaryAsync(bool forceRefresh = false)
        {
            var dashboard = await _dashboardRepository.GetCurrentAsync();

            if (dashboard != null && !forceRefresh && !dashboard.NeedsRefresh())
                return dashboard.CurrentSummary;

            var summary = new DashboardSummary
            {
                GeneratedAt = DateTime.UtcNow,
                SubscriptionMetrics = await GetSubscriptionMetricsAsync(),
                RevenueMetrics = await GetRevenueMetricsAsync(),
                ShopMetrics = await GetShopMetricsAsync(),
                UserMetrics = await GetUserMetricsAsync(),
                BannerMetrics = await GetBannerMetricsAsync(),
                SystemHealth = await GetSystemHealthAsync()
            };

            var dashboardEntity = new AdminDashboard();
            dashboardEntity.UpdateSummary(summary);
            await _dashboardRepository.CreateOrUpdateAsync(dashboardEntity);

            return summary;
        }

        public async Task<SubscriptionMetrics> GetSubscriptionMetricsAsync()
        {
            var allSubscriptions = await _subscriptionRepository.GetAllAsync();
            var activeSubscriptions = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.Active);
            var trialSubscriptions = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.Trial);
            var paymentFailed = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.PaymentFailed);
            var suspended = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.Suspended);

            var expiringInDays = allSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active &&
                           (s.RenewalDate - DateTime.UtcNow).TotalDays <= 7)
                .Count();

            var cancelledThisMonth = allSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Cancelled &&
                           s.CancellationDate.HasValue &&
                           s.CancellationDate.Value >= DateTime.UtcNow.AddDays(-30))
                .Count();

            var avgValue = activeSubscriptions.Any()
                ? activeSubscriptions.Average(s => s.CurrentPrice)
                : 0m;

            return new SubscriptionMetrics
            {
                TotalSubscriptions = allSubscriptions.Count,
                ActiveSubscriptions = activeSubscriptions.Count,
                TrialSubscriptions = trialSubscriptions.Count,
                ExpiringInDays = expiringInDays,
                PaymentFailedCount = paymentFailed.Count,
                SuspendedCount = suspended.Count,
                CancelledThisMonth = cancelledThisMonth,
                AverageSubscriptionValue = avgValue
            };
        }

        public async Task<RevenueMetrics> GetRevenueMetricsAsync()
        {
            var billingService = new BillingService(
                _subscriptionRepository,
                _invoiceRepository,
                _planRepository);

            var mrr = await billingService.CalculateMonthlyRecurringRevenueAsync();
            var arr = await billingService.CalculateAnnualRecurringRevenueAsync();
            var (monthlyRev, annualRev) = await billingService.GetRevenueAsync();
            var metrics = await billingService.GetBillingMetricsAsync();

            var allSubscriptions = await _subscriptionRepository.GetAllAsync();
            var activeCount = allSubscriptions.Where(s => s.Status == SubscriptionStatus.Active).Count();
            var avgRevenue = activeCount > 0 ? mrr / activeCount : 0m;

            return new RevenueMetrics
            {
                MonthlyCurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                MonthlyRevenue = monthlyRev,
                AnnualRevenue = annualRev,
                AverageRevenuePerSubscription = avgRevenue,
                CollectionRate = metrics.CollectionRate,
                PendingPayments = metrics.DraftInvoices,
                TotalOverdueAmount = metrics.OverdueInvoices * 100m // Simplified: multiply count by estimated average
            };
        }

        public async Task<ShopMetrics> GetShopMetricsAsync()
        {
            var shops = await _metricsRepository.GetShopCountsAsync();
            var totalUsers = await _metricsRepository.GetUserTotalCountAsync();
            var avgShopsPerUser = totalUsers > 0 ? (decimal)shops.Total / totalUsers : 0m;

            return new ShopMetrics
            {
                TotalShops = shops.Total,
                ActiveShops = shops.Active,
                InactiveShops = shops.Total - shops.Active,
                ShopsWithActiveSubscription = shops.WithActiveSubscription,
                TopLevelShops = shops.TopLevel,
                ChildShopsCount = shops.Total - shops.TopLevel,
                AverageShopsPerUser = avgShopsPerUser
            };
        }

        public async Task<UserMetrics> GetUserMetricsAsync()
        {
            var users = await _metricsRepository.GetUserCountsAsync();

            return new UserMetrics
            {
                TotalUsers = users.Total,
                ActiveUsers = users.Active,
                InactiveUsers = users.Total - users.Active,
                AdminCount = users.AdminRole,
                ShopOwnerCount = users.ShopOwnerRole,
                SalesExecutiveCount = users.SalesExecutiveRole,
                UsersWithFailedLogin = users.WithFailedLogin,
                LockedOutUsers = users.LockedOut
            };
        }

        public async Task<BannerMetrics> GetBannerMetricsAsync()
        {
            var banners = await _metricsRepository.GetBannerCountsAsync();

            return new BannerMetrics
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

        public async Task<SystemHealthMetrics> GetSystemHealthAsync()
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);

            var errorLogs = await _metricsRepository.GetSubscriptionCountAsync(); // Placeholder
            var warningLogs = await _metricsRepository.GetInvoiceCountAsync(); // Placeholder

            return new SystemHealthMetrics
            {
                IsDatabaseHealthy = true,
                PendingMigrations = 0,
                DatabaseSizeBytes = 1024 * 1024 * 100, // 100MB placeholder
                ErrorLogsLastHour = errorLogs,
                WarningLogsLastHour = warningLogs,
                LastHealthCheckTime = now,
                AverageResponseTimeMs = 45.5,
                ActiveConnections = 10
            };
        }

        public async Task<List<DashboardTrend>> GetTrendsAsync(int days = 30)
        {
            var trends = new List<DashboardTrend>();
            var snapshots = await _dashboardRepository.GetMetricSnapshotsAsync("subscription_count", days);

            if (snapshots.Count >= 2)
            {
                var current = snapshots.Last().MetricValue;
                var previous = snapshots.First().MetricValue;
                var change = ((current - previous) / previous) * 100;

                trends.Add(new DashboardTrend
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

        public async Task<List<TopShopByRevenue>> GetTopShopsByRevenueAsync(int limit = 10)
        {
            var invoices = await _invoiceRepository.GetByStatusAsync(InvoiceStatus.Paid);

            var topShops = invoices
                .GroupBy(i => i.ShopId)
                .Select(g => new
                {
                    ShopId = g.Key,
                    Revenue = g.Sum(i => i.Amount)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(limit)
                .ToList();

            var result = new List<TopShopByRevenue>();
            foreach (var shop in topShops)
            {
                var shopEntity = await _shopRepository.GetByIdAsync(shop.ShopId);
                if (shopEntity != null)
                {
                    var subs = await _subscriptionRepository.GetActiveByShopIdAsync(shop.ShopId);
                    var avgSubValue = subs.Any() ? subs.Average(s => s.CurrentPrice) : 0m;

                    result.Add(new TopShopByRevenue
                    {
                        ShopId = shop.ShopId,
                        ShopName = shopEntity.Name,
                        MonthlyRevenue = shop.Revenue,
                        SubscriptionCount = subs.Count,
                        AverageSubscriptionValue = avgSubValue
                    });
                }
            }

            return result;
        }

        public async Task<List<SubscriptionPlanDistribution>> GetSubscriptionDistributionAsync()
        {
            var subscriptions = await _subscriptionRepository.GetAllAsync();
            var plans = await _metricsRepository.GetAllPlansAsync();
            var totalSubs = subscriptions.Count;

            var distribution = plans
                .GroupJoin(subscriptions,
                    p => p.Id,
                    s => s.PlanId,
                    (p, subs) => new SubscriptionPlanDistribution
                    {
                        PlanName = p.Name,
                        Count = subs.Count(),
                        Percentage = totalSubs > 0 ? (subs.Count() / (decimal)totalSubs) * 100 : 0m,
                        TotalRevenue = subs.Sum(s => s.CurrentPrice)
                    })
                .Where(d => d.Count > 0)
                .OrderByDescending(d => d.Count)
                .ToList();

            return distribution;
        }

        public async Task<PaymentHealthMetrics> GetPaymentHealthAsync()
        {
            var payments = await _metricsRepository.GetPaymentCountsAsync();

            var successRate = payments.Total > 0 ? (payments.Paid / (decimal)payments.Total) * 100 : 0m;

            return new PaymentHealthMetrics
            {
                SuccessfulPayments = payments.Paid,
                FailedPayments = payments.Overdue,
                RefundedPayments = payments.Refunded,
                SuccessRate = successRate,
                AveragePaymentAmount = payments.AverageAmount,
                LastPaymentTime = payments.LastPaidDate ?? DateTime.UtcNow,
                PaymentsInGracePeriod = payments.SubscriptionsInGracePeriod
            };
        }

        public async Task CreateSystemAlertAsync(string alertType, string message, AlertSeverity severity)
        {
            var alert = new AdminDashboardAlert
            {
                AlertType = alertType,
                Message = message,
                Severity = severity
            };

            await _dashboardRepository.CreateAlertAsync(alert);
        }

        public async Task<List<AdminDashboardAlert>> GetActiveAlertsAsync()
        {
            return await _dashboardRepository.GetActiveAlertsAsync();
        }
    }
}
