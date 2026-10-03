namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class ShopAdRepository : IShopAdRepository
{
    private readonly ApplicationDbContext _context;

    public ShopAdRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ShopAd?> GetByIdAsync(Guid id) => _context.ShopAds.FirstOrDefaultAsync(a => a.Id == id);

    public async Task AddAsync(ShopAd ad, ShopAdEvent firstEvent)
    {
        _context.ShopAds.Add(ad);
        _context.ShopAdEvents.Add(firstEvent);
        await _context.SaveChangesAsync();
    }

    public async Task SaveAsync(ShopAd ad, ShopAdEvent? newEvent)
    {
        if (newEvent != null) _context.ShopAdEvents.Add(newEvent);
        await _context.SaveChangesAsync();
    }

    public Task<List<ShopAd>> FindSlotHoldersAsync(Guid shopId, DateTime fromUtc, DateTime toUtc) =>
        _context.ShopAds.AsNoTracking()
            .Where(a => a.ShopId == shopId && (a.Status == ShopAdStatus.PendingApproval || a.Status == ShopAdStatus.PendingCompliance || a.Status == ShopAdStatus.Approved)
                        && a.StartAt < toUtc && a.EndAt > fromUtc)
            .ToListAsync();

    public async Task<List<ShopAd>> ListAsync(Guid? shopId, DateTime? fromUtc, DateTime? toUtc, bool holdingSlotOnly)
    {
        var query = _context.ShopAds.AsNoTracking().AsQueryable();
        if (shopId != null) query = query.Where(a => a.ShopId == shopId);
        if (fromUtc != null) query = query.Where(a => a.EndAt > fromUtc);
        if (toUtc != null) query = query.Where(a => a.StartAt < toUtc);
        if (holdingSlotOnly) query = query.Where(a => a.Status == ShopAdStatus.PendingApproval || a.Status == ShopAdStatus.PendingCompliance || a.Status == ShopAdStatus.Approved);
        return await query.OrderByDescending(a => a.StartAt).ThenBy(a => a.Id).Take(500).ToListAsync();
    }

    public Task<List<ShopAd>> ListApprovedAsync(Guid? shopId, DateTime fromUtc, DateTime toUtc) =>
        _context.ShopAds.AsNoTracking()
            .Where(a => (shopId == null || a.ShopId == shopId) && a.DecidedAt != null
                        && (a.Status == ShopAdStatus.Approved || a.Status == ShopAdStatus.Cancelled || a.Status == ShopAdStatus.Overridden)
                        && a.StartAt < toUtc && a.EndAt > fromUtc)
            .ToListAsync();

    public Task<List<ShopAd>> ListCreatedAsync(DateTime fromUtc, DateTime toUtc) =>
        _context.ShopAds.AsNoTracking().Where(a => a.CreatedAt >= fromUtc && a.CreatedAt < toUtc).OrderBy(a => a.CreatedAt).Take(5000).ToListAsync();

    public Task<List<ShopAdEvent>> GetEventsAsync(Guid adId) =>
        _context.ShopAdEvents.AsNoTracking().Where(e => e.ShopAdId == adId).OrderBy(e => e.At).ToListAsync();
}
