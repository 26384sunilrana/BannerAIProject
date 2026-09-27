namespace BannerService.Domain.ValueObjects
{
    public class ShopSubscriptionMetrics
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int ExpiringInDays { get; set; }
        public int PaymentFailedCount { get; set; }
        public int SuspendedCount { get; set; }
        public decimal AverageSubscriptionValue { get; set; }
    }

    public class ShopRevenueMetrics
    {
        public decimal MonthlyCurringRevenue { get; set; }
        public decimal AnnualRecurringRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal AnnualRevenue { get; set; }
        public decimal AverageRevenuePerSubscription { get; set; }
        public decimal CollectionRate { get; set; }
        public int PendingPayments { get; set; }
        public decimal TotalOverdueAmount { get; set; }
    }

    public class ShopBannerMetrics
    {
        public int TotalBanners { get; set; }
        public int PublishedBanners { get; set; }
        public int DraftBanners { get; set; }
        public int BannersWithVersionControl { get; set; }
        public long TotalMediaStorageBytes { get; set; }
        public int TotalComponents { get; set; }
        public int TotalEffects { get; set; }
        public int TotalCarousels { get; set; }
    }

    public class ShopUserMetrics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int UsersWithFailedLogin { get; set; }
        public int LockedOutUsers { get; set; }
    }

    public class ShopDashboardMetrics
    {
        public DateTime GeneratedAt { get; set; }
        public ShopSubscriptionMetrics SubscriptionMetrics { get; set; } = new();
        public ShopRevenueMetrics RevenueMetrics { get; set; } = new();
        public ShopBannerMetrics BannerMetrics { get; set; } = new();
        public ShopUserMetrics UserMetrics { get; set; } = new();
    }

    public class ShopDashboardTrend
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = "STABLE"; // UP, DOWN, STABLE
        public DateTime MeasuredAt { get; set; }
    }

    public class TopBannerByPerformance
    {
        public Guid BannerId { get; set; }
        public string BannerName { get; set; } = string.Empty;
        public int ComponentCount { get; set; }
        public int EffectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
