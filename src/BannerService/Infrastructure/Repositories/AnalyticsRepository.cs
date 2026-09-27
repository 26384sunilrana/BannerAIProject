namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardReport?> GetReportByIdAsync(Guid reportId)
        {
            return await _context.Set<DashboardReport>()
                .FirstOrDefaultAsync(r => r.Id == reportId);
        }

        public async Task<List<DashboardReport>> GetShopReportsAsync(Guid shopId)
        {
            return await _context.Set<DashboardReport>()
                .Where(r => r.ShopId == shopId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DashboardReport>> GetReportsByTypeAsync(ReportType type)
        {
            return await _context.Set<DashboardReport>()
                .Where(r => r.Type == type)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DashboardReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Set<DashboardReport>()
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DashboardReport>> GetActiveReportsAsync()
        {
            return await _context.Set<DashboardReport>()
                .Where(r => r.Status != ReportStatus.Archived)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<DashboardReport> CreateReportAsync(DashboardReport report)
        {
            _context.Set<DashboardReport>().Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<DashboardReport> UpdateReportAsync(DashboardReport report)
        {
            _context.Set<DashboardReport>().Update(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<bool> DeleteReportAsync(Guid reportId)
        {
            var report = await GetReportByIdAsync(reportId);
            if (report == null)
                return false;

            _context.Set<DashboardReport>().Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DashboardReport>> GetReportsByStatusAsync(ReportStatus status)
        {
            return await _context.Set<DashboardReport>()
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DashboardReport>> GetExpiringReportsAsync(int retentionDays = 90)
        {
            var expiryDate = DateTime.UtcNow.AddDays(-retentionDays);
            return await _context.Set<DashboardReport>()
                .Where(r => r.CreatedAt < expiryDate && r.Status == ReportStatus.Archived)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<AnalyticsEvent> CreateEventAsync(AnalyticsEvent evt)
        {
            _context.Set<AnalyticsEvent>().Add(evt);
            await _context.SaveChangesAsync();
            return evt;
        }

        public async Task<List<AnalyticsEvent>> GetEventsByShopAsync(Guid shopId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<AnalyticsEvent>()
                .Where(e => e.ShopId == shopId);

            if (startDate.HasValue)
                query = query.Where(e => e.OccurredAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.OccurredAt <= endDate.Value);

            return await query.OrderByDescending(e => e.OccurredAt).ToListAsync();
        }

        public async Task<List<AnalyticsEvent>> GetEventsByTypeAsync(EventType eventType)
        {
            return await _context.Set<AnalyticsEvent>()
                .Where(e => e.EventType == eventType)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync();
        }

        public async Task<List<AnalyticsEvent>> GetEventsByResourceAsync(Guid resourceId, string resourceType)
        {
            return await _context.Set<AnalyticsEvent>()
                .Where(e => e.ResourceId == resourceId && e.ResourceType == resourceType)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync();
        }

        public async Task<List<AnalyticsEvent>> GetEventsByUserAsync(Guid userId)
        {
            return await _context.Set<AnalyticsEvent>()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync();
        }

        public async Task<List<AnalyticsEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Set<AnalyticsEvent>()
                .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync();
        }

        public async Task<int> GetEventCountAsync(Guid shopId)
        {
            return await _context.Set<AnalyticsEvent>()
                .Where(e => e.ShopId == shopId)
                .CountAsync();
        }

        public async Task<int> GetEventCountByTypeAsync(EventType eventType)
        {
            return await _context.Set<AnalyticsEvent>()
                .Where(e => e.EventType == eventType)
                .CountAsync();
        }

        public async Task<bool> DeleteOldEventsAsync(int daysToKeep = 365)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var events = await _context.Set<AnalyticsEvent>()
                .Where(e => e.CreatedAt < cutoffDate)
                .ToListAsync();

            if (events.Any())
            {
                _context.Set<AnalyticsEvent>().RemoveRange(events);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
