namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboard?> GetCurrentAsync()
        {
            return await _context.Set<AdminDashboard>()
                .OrderByDescending(d => d.LastUpdatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<AdminDashboard> CreateOrUpdateAsync(AdminDashboard dashboard)
        {
            var existing = await GetCurrentAsync();

            if (existing != null)
            {
                existing.UpdateSummary(dashboard.CurrentSummary);
                _context.Set<AdminDashboard>().Update(existing);
            }
            else
            {
                _context.Set<AdminDashboard>().Add(dashboard);
            }

            await _context.SaveChangesAsync();
            return existing ?? dashboard;
        }

        public async Task<List<DashboardMetricSnapshot>> GetMetricSnapshotsAsync(string metricType, int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            return await _context.Set<DashboardMetricSnapshot>()
                .Where(s => s.MetricType == metricType && s.SnapshotDate >= fromDate)
                .OrderBy(s => s.SnapshotDate)
                .ToListAsync();
        }

        public async Task<DashboardMetricSnapshot> CreateSnapshotAsync(DashboardMetricSnapshot snapshot)
        {
            _context.Set<DashboardMetricSnapshot>().Add(snapshot);
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task<List<AdminDashboardAlert>> GetActiveAlertsAsync()
        {
            return await _context.Set<AdminDashboardAlert>()
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<AdminDashboardAlert> CreateAlertAsync(AdminDashboardAlert alert)
        {
            _context.Set<AdminDashboardAlert>().Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task<bool> ResolveAlertAsync(Guid alertId)
        {
            var alert = await _context.Set<AdminDashboardAlert>()
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null)
                return false;

            alert.Resolve();
            _context.Set<AdminDashboardAlert>().Update(alert);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AdminDashboardAlert>> GetAlertsAsync(DateTime fromDate, int limit = 100)
        {
            return await _context.Set<AdminDashboardAlert>()
                .Where(a => a.CreatedAt >= fromDate)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
