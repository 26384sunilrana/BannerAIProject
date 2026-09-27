namespace BannerService.Application.DTOs
{
    public class SubscriptionMetricsDto
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int ExpiringInDays { get; set; }
        public int PaymentFailedCount { get; set; }
        public int SuspendedCount { get; set; }
        public int CancelledThisMonth { get; set; }
        public decimal AverageSubscriptionValue { get; set; }
    }

    public class RevenueMetricsDto
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

    public class ShopMetricsDto
    {
        public int TotalShops { get; set; }
        public int ActiveShops { get; set; }
        public int InactiveShops { get; set; }
        public int ShopsWithActiveSubscription { get; set; }
        public int TopLevelShops { get; set; }
        public int ChildShopsCount { get; set; }
        public decimal AverageShopsPerUser { get; set; }
    }

    public class UserMetricsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminCount { get; set; }
        public int ShopOwnerCount { get; set; }
        public int SalesExecutiveCount { get; set; }
        public int UsersWithFailedLogin { get; set; }
        public int LockedOutUsers { get; set; }
    }

    public class BannerMetricsDto
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

    public class SystemHealthMetricsDto
    {
        public bool IsDatabaseHealthy { get; set; }
        public int PendingMigrations { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public int ErrorLogsLastHour { get; set; }
        public int WarningLogsLastHour { get; set; }
        public DateTime LastHealthCheckTime { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public int ActiveConnections { get; set; }
    }

    public class DashboardSummaryDto
    {
        public DateTime GeneratedAt { get; set; }
        public SubscriptionMetricsDto SubscriptionMetrics { get; set; } = new();
        public RevenueMetricsDto RevenueMetrics { get; set; } = new();
        public ShopMetricsDto ShopMetrics { get; set; } = new();
        public UserMetricsDto UserMetrics { get; set; } = new();
        public BannerMetricsDto BannerMetrics { get; set; } = new();
        public SystemHealthMetricsDto SystemHealth { get; set; } = new();
    }

    public class DashboardTrendDto
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = string.Empty;
        public DateTime MeasuredAt { get; set; }
    }

    public class TopShopByRevenueDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public decimal MonthlyRevenue { get; set; }
        public int SubscriptionCount { get; set; }
        public decimal AverageSubscriptionValue { get; set; }
    }

    public class SubscriptionPlanDistributionDto
    {
        public string PlanName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PaymentHealthMetricsDto
    {
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int RefundedPayments { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public DateTime LastPaymentTime { get; set; }
        public int PaymentsInGracePeriod { get; set; }
    }

    public class AdminDashboardAlertDto
    {
        public Guid Id { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class DashboardOverviewDto
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public List<AdminDashboardAlertDto> ActiveAlerts { get; set; } = new();
        public List<DashboardTrendDto> KeyTrends { get; set; } = new();
        public List<TopShopByRevenueDto> TopShops { get; set; } = new();
    }
}
