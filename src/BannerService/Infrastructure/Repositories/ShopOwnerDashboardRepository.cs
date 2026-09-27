namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class ShopOwnerDashboardRepository : IShopOwnerDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public ShopOwnerDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShopOwnerDashboard?> GetCurrentAsync(Guid shopId)
        {
            return await _context.Set<ShopOwnerDashboard>()
                .Where(d => d.ShopId == shopId)
                .OrderByDescending(d => d.LastUpdatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ShopOwnerDashboard> CreateOrUpdateAsync(ShopOwnerDashboard dashboard)
        {
            var existing = await GetCurrentAsync(dashboard.ShopId);

            if (existing != null)
            {
                existing.UpdateSummary(dashboard.CurrentSummary);
                _context.Set<ShopOwnerDashboard>().Update(existing);
            }
            else
            {
                _context.Set<ShopOwnerDashboard>().Add(dashboard);
            }

            await _context.SaveChangesAsync();
            return existing ?? dashboard;
        }

        public async Task<List<ShopDashboardMetricSnapshot>> GetMetricSnapshotsAsync(Guid shopId, string metricType, int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            return await _context.Set<ShopDashboardMetricSnapshot>()
                .Where(s => s.ShopId == shopId && s.MetricType == metricType && s.SnapshotDate >= fromDate)
                .OrderBy(s => s.SnapshotDate)
                .ToListAsync();
        }

        public async Task<ShopDashboardMetricSnapshot> CreateSnapshotAsync(ShopDashboardMetricSnapshot snapshot)
        {
            _context.Set<ShopDashboardMetricSnapshot>().Add(snapshot);
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task<List<ShopDashboardAlert>> GetActiveAlertsAsync(Guid shopId)
        {
            return await _context.Set<ShopDashboardAlert>()
                .Where(a => a.ShopId == shopId && !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<ShopDashboardAlert> CreateAlertAsync(ShopDashboardAlert alert)
        {
            _context.Set<ShopDashboardAlert>().Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task<bool> ResolveAlertAsync(Guid alertId)
        {
            var alert = await _context.Set<ShopDashboardAlert>()
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null)
                return false;

            alert.Resolve();
            _context.Set<ShopDashboardAlert>().Update(alert);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ShopDashboardAlert>> GetAlertsAsync(Guid shopId, DateTime fromDate, int limit = 100)
        {
            return await _context.Set<ShopDashboardAlert>()
                .Where(a => a.ShopId == shopId && a.CreatedAt >= fromDate)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
