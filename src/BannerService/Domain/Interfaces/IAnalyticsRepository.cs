namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;

    public interface IAnalyticsRepository
    {
        Task<DashboardReport?> GetReportByIdAsync(Guid reportId);
        Task<List<DashboardReport>> GetShopReportsAsync(Guid shopId);
        Task<List<DashboardReport>> GetReportsByTypeAsync(ReportType type);
        Task<List<DashboardReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<DashboardReport>> GetActiveReportsAsync();
        Task<DashboardReport> CreateReportAsync(DashboardReport report);
        Task<DashboardReport> UpdateReportAsync(DashboardReport report);
        Task<bool> DeleteReportAsync(Guid reportId);
        Task<List<DashboardReport>> GetReportsByStatusAsync(ReportStatus status);
        Task<List<DashboardReport>> GetExpiringReportsAsync(int retentionDays = 90);

        Task<AnalyticsEvent> CreateEventAsync(AnalyticsEvent evt);
        Task<List<AnalyticsEvent>> GetEventsByShopAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AnalyticsEvent>> GetEventsByTypeAsync(EventType eventType);
        Task<List<AnalyticsEvent>> GetEventsByResourceAsync(Guid resourceId, string resourceType);
        Task<List<AnalyticsEvent>> GetEventsByUserAsync(Guid userId);
        Task<List<AnalyticsEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetEventCountAsync(Guid shopId);
        Task<int> GetEventCountByTypeAsync(EventType eventType);
        Task<bool> DeleteOldEventsAsync(int daysToKeep = 365);
    }
}
