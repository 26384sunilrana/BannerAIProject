namespace BannerService.Domain.ValueObjects
{
    public class SubscriptionMetrics
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

    public class RevenueMetrics
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

    public class ShopMetrics
    {
        public int TotalShops { get; set; }
        public int ActiveShops { get; set; }
        public int InactiveShops { get; set; }
        public int ShopsWithActiveSubscription { get; set; }
        public int TopLevelShops { get; set; }
        public int ChildShopsCount { get; set; }
        public decimal AverageShopsPerUser { get; set; }
    }

    public class UserMetrics
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

    public class BannerMetrics
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

    public class SystemHealthMetrics
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

    public class DashboardSummary
    {
        public DateTime GeneratedAt { get; set; }
        public SubscriptionMetrics SubscriptionMetrics { get; set; } = new();
        public RevenueMetrics RevenueMetrics { get; set; } = new();
        public ShopMetrics ShopMetrics { get; set; } = new();
        public UserMetrics UserMetrics { get; set; } = new();
        public BannerMetrics BannerMetrics { get; set; } = new();
        public SystemHealthMetrics SystemHealth { get; set; } = new();
    }

    public class DashboardTrend
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = string.Empty; // "UP", "DOWN", "STABLE"
        public DateTime MeasuredAt { get; set; }
    }

    public class TopShopByRevenue
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public decimal MonthlyRevenue { get; set; }
        public int SubscriptionCount { get; set; }
        public decimal AverageSubscriptionValue { get; set; }
    }

    public class SubscriptionPlanDistribution
    {
        public string PlanName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PaymentHealthMetrics
    {
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int RefundedPayments { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public DateTime LastPaymentTime { get; set; }
        public int PaymentsInGracePeriod { get; set; }
    }
}
