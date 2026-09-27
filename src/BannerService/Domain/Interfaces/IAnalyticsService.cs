namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;
    using Domain.ValueObjects;

    public interface IAnalyticsService
    {
        Task<DashboardReport> GenerateReportAsync(Guid shopId, ReportType type, DateTime startDate, DateTime endDate, Guid userId);
        Task<DashboardReport> GetReportAsync(Guid reportId);
        Task<List<DashboardReport>> GetShopReportsAsync(Guid shopId);
        Task<DashboardReport> ExportReportAsync(Guid reportId, string format);
        Task<bool> DeleteReportAsync(Guid reportId);
        Task RecordEventAsync(Guid shopId, EventType eventType, Guid? userId = null, Guid? resourceId = null, string? resourceType = null, Dictionary<string, object>? properties = null);
        Task<List<AnalyticsEvent>> GetEventsAsync(Guid shopId, DateTime startDate, DateTime endDate, EventType? filterByType = null);
        Task<Dictionary<string, int>> GetEventCountByTypeAsync(Guid shopId, DateTime startDate, DateTime endDate);
        Task<decimal> GetConversionRateAsync(Guid shopId, DateTime startDate, DateTime endDate);
        Task<decimal> GetEngagementRateAsync(Guid shopId, DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetMetricsComparisonAsync(Guid shopId, DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End);
        Task<List<ReportMetric>> GetTrendAnalysisAsync(Guid shopId, string metricName, int dayCount);
        Task<DashboardReport> GenerateComparisonReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId);
        Task<DashboardReport> GenerateSubscriptionReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId);
        Task<DashboardReport> GenerateRevenueReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId);
        Task<DashboardReport> GenerateBannerReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId);
        Task<DashboardReport> GenerateAdvertisementReportAsync(Guid shopId, DateTime startDate, DateTime endDate, Guid userId);
        Task CleanupOldEventsAsync(int daysToKeep = 365);
        Task CleanupExpiredReportsAsync(int retentionDays = 90);
    }
}
