namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Infrastructure.Data;

public class BannerVersionRepository : IBannerVersionRepository
{
    private readonly ApplicationDbContext _context;

    public BannerVersionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BannerVersion> SaveAsync(BannerVersion version)
    {
        _context.BannerVersions.Add(version);
        await _context.SaveChangesAsync();
        return version;
    }

    public async Task<List<BannerVersion>> GetVersionsAsync(Guid bannerId, Guid shopId)
    {
        return await _context.BannerVersions
            .Where(v => v.BannerId == bannerId && v.ShopId == shopId && v.IsActive)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }

    public async Task<BannerVersion?> GetVersionAsync(Guid bannerId, int versionNumber, Guid shopId)
    {
        return await _context.BannerVersions
            .FirstOrDefaultAsync(v => v.BannerId == bannerId &&
                v.VersionNumber == versionNumber &&
                v.ShopId == shopId &&
                v.IsActive);
    }

    public async Task<int> DeactivateOldVersionsAsync(Guid bannerId, Guid shopId, int keep)
    {
        var old = await _context.BannerVersions
            .Where(v => v.BannerId == bannerId && v.ShopId == shopId && v.IsActive)
            .OrderByDescending(v => v.VersionNumber)
            .Skip(keep)
            .ToListAsync();

        foreach (var version in old)
            version.IsActive = false;

        await _context.SaveChangesAsync();
        return old.Count;
    }

    public async Task<int> GetNextVersionNumberAsync(Guid bannerId, Guid shopId)
    {
        var maxVersion = await _context.BannerVersions
            .Where(v => v.BannerId == bannerId && v.ShopId == shopId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;
        return maxVersion + 1;
    }
}
