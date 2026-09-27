namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IAdminDashboardRepository
    {
        Task<AdminDashboard?> GetCurrentAsync();
        Task<AdminDashboard> CreateOrUpdateAsync(AdminDashboard dashboard);
        Task<List<DashboardMetricSnapshot>> GetMetricSnapshotsAsync(string metricType, int days = 30);
        Task<DashboardMetricSnapshot> CreateSnapshotAsync(DashboardMetricSnapshot snapshot);
        Task<List<AdminDashboardAlert>> GetActiveAlertsAsync();
        Task<AdminDashboardAlert> CreateAlertAsync(AdminDashboardAlert alert);
        Task<bool> ResolveAlertAsync(Guid alertId);
        Task<List<AdminDashboardAlert>> GetAlertsAsync(DateTime fromDate, int limit = 100);
    }
}
