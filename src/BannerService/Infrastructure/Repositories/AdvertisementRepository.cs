namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class AdvertisementRepository : IAdvertisementRepository
    {
        private readonly ApplicationDbContext _context;

        public AdvertisementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Advertisement?> GetByIdAsync(Guid adId)
        {
            return await _context.Set<Advertisement>()
                .FirstOrDefaultAsync(a => a.Id == adId);
        }

        public async Task<List<Advertisement>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.ShopId == shopId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetByStatusAsync(AdStatus status)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetActiveAdsAsync()
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.Status == AdStatus.Active && a.IsActive)
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetByCreatorAsync(Guid userId)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.CreatedByUserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetExpiringAdsAsync(int daysThreshold = 7)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
            return await _context.Set<Advertisement>()
                .Where(a => a.Status == AdStatus.Active && a.EndDate <= thresholdDate && a.EndDate > DateTime.UtcNow)
                .OrderBy(a => a.EndDate)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetAllAsync()
        {
            return await _context.Set<Advertisement>()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Advertisement> CreateAsync(Advertisement ad)
        {
            _context.Set<Advertisement>().Add(ad);
            await _context.SaveChangesAsync();
            return ad;
        }

        public async Task<Advertisement> UpdateAsync(Advertisement ad)
        {
            _context.Set<Advertisement>().Update(ad);
            await _context.SaveChangesAsync();
            return ad;
        }

        public async Task<bool> DeleteAsync(Guid adId)
        {
            var ad = await GetByIdAsync(adId);
            if (ad == null)
                return false;

            _context.Set<Advertisement>().Remove(ad);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Advertisement>> GetByTypeAsync(AdType type)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.Type == type)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetAdsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.StartDate >= startDate && a.EndDate <= endDate)
                .OrderBy(a => a.StartDate)
                .ToListAsync();
        }

        public async Task<int> GetCountByShopAsync(Guid shopId)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.ShopId == shopId)
                .CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(AdStatus status)
        {
            return await _context.Set<Advertisement>()
                .Where(a => a.Status == status)
                .CountAsync();
        }
    }
}
