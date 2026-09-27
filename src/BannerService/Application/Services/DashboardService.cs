namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;
    using Microsoft.EntityFrameworkCore;
    using Infrastructure.Data;

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminDashboardRepository _dashboardRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public DashboardService(
            ApplicationDbContext context,
            IAdminDashboardRepository dashboardRepository,
            ISubscriptionRepository subscriptionRepository,
            IInvoiceRepository invoiceRepository,
            IShopRepository shopRepository,
            IUserRepository userRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _context = context;
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
            var allShops = _context.Set<Shop>().ToList();
            var topLevelShops = allShops.Where(s => s.ParentShopId == null).Count();
            var childShops = allShops.Count - topLevelShops;

            var activeShops = allShops.Where(s => s.Status == ShopStatus.Active).Count();
            var inactiveShops = allShops.Count - activeShops;

            var allUsers = _context.Set<User>().ToList();
            var avgShopsPerUser = allUsers.Any() ? (decimal)allShops.Count / allUsers.Count : 0m;

            var shopsWithSubscription = _context.Set<Subscription>()
                .Where(s => s.Status == SubscriptionStatus.Active)
                .Select(s => s.ShopId)
                .Distinct()
                .Count();

            return new ShopMetrics
            {
                TotalShops = allShops.Count,
                ActiveShops = activeShops,
                InactiveShops = inactiveShops,
                ShopsWithActiveSubscription = shopsWithSubscription,
                TopLevelShops = topLevelShops,
                ChildShopsCount = childShops,
                AverageShopsPerUser = avgShopsPerUser
            };
        }

        public async Task<UserMetrics> GetUserMetricsAsync()
        {
            var allUsers = _context.Set<User>().ToList();
            var activeUsers = allUsers.Where(u => u.IsActive).Count();
            var inactiveUsers = allUsers.Count - activeUsers;

            var userRoles = _context.Set<UserRole>();
            var adminCount = userRoles.Where(ur => ur.RoleId == "1").Count();
            var ownerCount = userRoles.Where(ur => ur.RoleId == "2").Count();
            var executiveCount = userRoles.Where(ur => ur.RoleId == "3").Count();

            var failedLoginUsers = allUsers.Where(u => u.LoginAttempts > 0).Count();
            var lockedOutUsers = allUsers.Where(u => u.IsLockedOut).Count();

            return new UserMetrics
            {
                TotalUsers = allUsers.Count,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                AdminCount = adminCount,
                ShopOwnerCount = ownerCount,
                SalesExecutiveCount = executiveCount,
                UsersWithFailedLogin = failedLoginUsers,
                LockedOutUsers = lockedOutUsers
            };
        }

        public async Task<BannerMetrics> GetBannerMetricsAsync()
        {
            var banners = await _context.Set<Banner>().ToListAsync();
            var components = await _context.Set<Component>().ToListAsync();
            var effects = await _context.Set<Effect>().ToListAsync();
            var carousels = await _context.Set<Carousel>().ToListAsync();
            var mediaFiles = await _context.Set<MediaFile>().ToListAsync();

            var publishedBanners = banners.Where(b => b.IsPublished).Count();
            var draftBanners = banners.Count - publishedBanners;

            var bannersWithVersions = _context.Set<BannerVersion>()
                .Select(v => v.BannerId)
                .Distinct()
                .Count();

            var totalMediaSize = mediaFiles.Sum(m => m.SizeBytes);

            return new BannerMetrics
            {
                TotalBanners = banners.Count,
                PublishedBanners = publishedBanners,
                DraftBanners = draftBanners,
                BannersWithVersionControl = bannersWithVersions,
                TotalMediaStorageBytes = totalMediaSize,
                TotalComponents = components.Count,
                TotalEffects = effects.Count,
                TotalCarousels = carousels.Count
            };
        }

        public async Task<SystemHealthMetrics> GetSystemHealthAsync()
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);

            var errorLogs = _context.Set<Subscription>().Count(); // Placeholder
            var warningLogs = _context.Set<Invoice>().Count(); // Placeholder

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
            var plans = await _context.Set<SubscriptionPlan>().ToListAsync();
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
            var allInvoices = _context.Set<Invoice>();
            var successfulPayments = allInvoices.Where(i => i.Status == InvoiceStatus.Paid).Count();
            var failedPayments = allInvoices.Where(i => i.Status == InvoiceStatus.Overdue).Count();
            var refundedPayments = allInvoices.Where(i => i.Status == InvoiceStatus.Refunded).Count();
            var totalPayments = allInvoices.Count();

            var successRate = totalPayments > 0 ? (successfulPayments / (decimal)totalPayments) * 100 : 0m;
            var avgAmount = allInvoices.Any() ? allInvoices.Average(i => i.Amount) : 0m;

            var lastPayment = allInvoices
                .Where(i => i.PaidDate.HasValue)
                .OrderByDescending(i => i.PaidDate)
                .FirstOrDefault();

            var subscriptionsInGrace = await _context.Set<Subscription>()
                .Where(s => s.Status == SubscriptionStatus.GracePeriod)
                .CountAsync();

            return new PaymentHealthMetrics
            {
                SuccessfulPayments = successfulPayments,
                FailedPayments = failedPayments,
                RefundedPayments = refundedPayments,
                SuccessRate = successRate,
                AveragePaymentAmount = avgAmount,
                LastPaymentTime = lastPayment?.PaidDate ?? DateTime.UtcNow,
                PaymentsInGracePeriod = subscriptionsInGrace
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
