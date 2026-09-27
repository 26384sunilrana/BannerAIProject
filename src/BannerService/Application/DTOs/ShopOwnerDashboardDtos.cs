namespace BannerService.Application.DTOs
{
    public class ShopSubscriptionMetricsDto
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int ExpiringInDays { get; set; }
        public int PaymentFailedCount { get; set; }
        public int SuspendedCount { get; set; }
        public decimal AverageSubscriptionValue { get; set; }
    }

    public class ShopRevenueMetricsDto
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

    public class ShopBannerMetricsDto
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

    public class ShopUserMetricsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int UsersWithFailedLogin { get; set; }
        public int LockedOutUsers { get; set; }
    }

    public class ShopDashboardSummaryDto
    {
        public DateTime GeneratedAt { get; set; }
        public ShopSubscriptionMetricsDto SubscriptionMetrics { get; set; } = new();
        public ShopRevenueMetricsDto RevenueMetrics { get; set; } = new();
        public ShopBannerMetricsDto BannerMetrics { get; set; } = new();
        public ShopUserMetricsDto UserMetrics { get; set; } = new();
    }

    public class ShopDashboardTrendDto
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = "STABLE";
        public DateTime MeasuredAt { get; set; }
    }

    public class TopBannerByPerformanceDto
    {
        public Guid BannerId { get; set; }
        public string BannerName { get; set; } = string.Empty;
        public int ComponentCount { get; set; }
        public int EffectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class ShopDashboardOverviewDto
    {
        public ShopDashboardSummaryDto Summary { get; set; } = new();
        public List<ShopDashboardAlertDto> ActiveAlerts { get; set; } = new();
        public List<ShopDashboardTrendDto> KeyTrends { get; set; } = new();
        public List<TopBannerByPerformanceDto> TopBanners { get; set; } = new();
    }

    public class ShopDashboardAlertDto
    {
        public Guid Id { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
