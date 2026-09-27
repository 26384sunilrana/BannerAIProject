namespace BannerService.Domain.Interfaces
{
    using Domain.ValueObjects;
    using Domain.Entities;

    public interface IShopOwnerDashboardService
    {
        Task<ShopDashboardMetrics> GetDashboardSummaryAsync(Guid shopId, bool forceRefresh = false);
        Task<ShopSubscriptionMetrics> GetSubscriptionMetricsAsync(Guid shopId);
        Task<ShopRevenueMetrics> GetRevenueMetricsAsync(Guid shopId);
        Task<ShopBannerMetrics> GetBannerMetricsAsync(Guid shopId);
        Task<ShopUserMetrics> GetUserMetricsAsync(Guid shopId);
        Task<List<ShopDashboardTrend>> GetTrendsAsync(Guid shopId, int days = 30);
        Task<List<TopBannerByPerformance>> GetTopBannersAsync(Guid shopId, int limit = 10);
        Task<List<ShopDashboardAlert>> GetActiveAlertsAsync(Guid shopId);
        Task CreateShopAlertAsync(Guid shopId, string alertType, string message, ShopAlertSeverity severity);
    }
}
