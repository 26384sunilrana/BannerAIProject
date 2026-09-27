namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;

    public interface IShopOwnerDashboardRepository
    {
        Task<ShopOwnerDashboard?> GetCurrentAsync(Guid shopId);
        Task<ShopOwnerDashboard> CreateOrUpdateAsync(ShopOwnerDashboard dashboard);
        Task<List<ShopDashboardMetricSnapshot>> GetMetricSnapshotsAsync(Guid shopId, string metricType, int days = 30);
        Task<ShopDashboardMetricSnapshot> CreateSnapshotAsync(ShopDashboardMetricSnapshot snapshot);
        Task<List<ShopDashboardAlert>> GetActiveAlertsAsync(Guid shopId);
        Task<ShopDashboardAlert> CreateAlertAsync(ShopDashboardAlert alert);
        Task<bool> ResolveAlertAsync(Guid alertId);
        Task<List<ShopDashboardAlert>> GetAlertsAsync(Guid shopId, DateTime fromDate, int limit = 100);
    }
}
