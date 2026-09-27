namespace BannerService.Domain.Interfaces
{
    using Entities;
    using ValueObjects;

    public interface IDashboardService
    {
        Task<DashboardSummary> GetDashboardSummaryAsync(bool forceRefresh = false);
        Task<SubscriptionMetrics> GetSubscriptionMetricsAsync();
        Task<RevenueMetrics> GetRevenueMetricsAsync();
        Task<ShopMetrics> GetShopMetricsAsync();
        Task<UserMetrics> GetUserMetricsAsync();
        Task<BannerMetrics> GetBannerMetricsAsync();
        Task<SystemHealthMetrics> GetSystemHealthAsync();
        Task<List<DashboardTrend>> GetTrendsAsync(int days = 30);
        Task<List<TopShopByRevenue>> GetTopShopsByRevenueAsync(int limit = 10);
        Task<List<SubscriptionPlanDistribution>> GetSubscriptionDistributionAsync();
        Task<PaymentHealthMetrics> GetPaymentHealthAsync();
        Task<List<AdminDashboardAlert>> GetActiveAlertsAsync();
        Task CreateSystemAlertAsync(string alertType, string message, AlertSeverity severity);
    }
}
