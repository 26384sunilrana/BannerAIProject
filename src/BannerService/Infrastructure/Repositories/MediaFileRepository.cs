namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Infrastructure.Data;

public class MediaFileRepository : IMediaFileRepository
{
    private readonly ApplicationDbContext _context;

    public MediaFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaFile> SaveAsync(MediaFile mediaFile)
    {
        _context.MediaFiles.Add(mediaFile);
        await _context.SaveChangesAsync();
        return mediaFile;
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id, Guid shopId)
    {
        return await _context.MediaFiles
            .FirstOrDefaultAsync(m => m.Id == id && m.ShopId == shopId);
    }

    public async Task<MediaFile?> GetByIdUnscopedAsync(Guid id)
    {
        return await _context.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<MediaFile>> GetByShopAsync(Guid shopId)
    {
        return await _context.MediaFiles
            .Where(m => m.ShopId == shopId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(MediaFile mediaFile)
    {
        _context.MediaFiles.Update(mediaFile);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid shopId)
    {
        var mediaFile = await GetByIdAsync(id, shopId);
        if (mediaFile != null)
        {
            _context.MediaFiles.Remove(mediaFile);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<(List<MediaFile> items, int total)> GetActivePageAsync(Guid shopId, int? fileType, string? search, int page, int pageSize)
    {
        var query = _context.MediaFiles.AsNoTracking()
            .Where(m => m.ShopId == shopId && m.Status == (int)MediaFileStatus.Active);

        if (fileType.HasValue)
            query = query.Where(m => m.FileType == fileType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m => m.FileName.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<MediaUsage> GetUsageAsync(Guid shopId)
    {
        var rows = await _context.MediaFiles.AsNoTracking()
            .Where(m => m.ShopId == shopId && m.Status == (int)MediaFileStatus.Active)
            .GroupBy(m => m.FileType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Bytes = g.Sum(m => m.SizeBytes) })
            .ToListAsync();

        return new MediaUsage(
            rows.Sum(r => r.Bytes),
            rows.Sum(r => r.Count),
            rows.Where(r => r.Type == (int)MediaFileType.Image).Sum(r => r.Count),
            rows.Where(r => r.Type == (int)MediaFileType.Video).Sum(r => r.Count));
    }

    public async Task<long> GetReservedBytesAsync(Guid shopId) =>
        await _context.MediaFiles.AsNoTracking()
            .Where(m => m.ShopId == shopId && (m.Status == (int)MediaFileStatus.Active || m.Status == (int)MediaFileStatus.Pending))
            .SumAsync(m => (long?)m.SizeBytes) ?? 0;

    public async Task<List<string>> FindUsingBannersAsync(Guid shopId, Guid mediaFileId)
    {
        var needle = mediaFileId.ToString();

        var current = await (from c in _context.Components
                             join b in _context.Banners on c.BannerId equals b.Id
                             where b.ShopId == shopId && c.PropertiesJson.Contains(needle)
                             select b.Name).Distinct().ToListAsync();

        var earlier = await (from v in _context.BannerVersions
                             join b in _context.Banners on v.BannerId equals b.Id
                             where b.ShopId == shopId && v.SnapshotJson.Contains(needle)
                             select b.Name).Distinct().ToListAsync();

        var names = current.Union(earlier).OrderBy(n => n).ToList();

        // the logo on the shop's default board counts as a use too
        if (await _context.Shops.AnyAsync(sh => sh.Id == shopId && sh.DefaultBoardLogoMediaId == mediaFileId))
            names.Add("the default board");

        // so does a picture on an ad that is waiting or approved and not over yet
        var now = DateTime.UtcNow;
        var ads = await _context.ShopAds
            .Where(ad => ad.ShopId == shopId && ad.MediaFileId == mediaFileId && ad.EndAt > now
                         && (ad.Status == ShopAdStatus.PendingApproval || ad.Status == ShopAdStatus.PendingCompliance || ad.Status == ShopAdStatus.Approved))
            .Select(ad => ad.Headline).ToListAsync();
        names.AddRange(ads.Select(h => $"the ad \"{h}\""));

        return names;
    }

    public async Task<List<MediaFile>> GetAbandonedAsync(DateTime pendingBefore)
    {
        return await _context.MediaFiles
            .Where(m => (m.Status == (int)MediaFileStatus.Pending && m.CreatedAt < pendingBefore)
                     || m.Status == (int)MediaFileStatus.Failed)
            .OrderBy(m => m.CreatedAt)
            .Take(500)
            .ToListAsync();
    }

    public async Task<List<MediaFile>> GetTombstonesAsync(DateTime deletedBefore)
    {
        return await _context.MediaFiles
            .Where(m => m.Status == (int)MediaFileStatus.Deleted && m.DeletedAt != null && m.DeletedAt < deletedBefore)
            .OrderBy(m => m.DeletedAt)
            .Take(500)
            .ToListAsync();
    }

    public async Task RemoveAsync(MediaFile mediaFile)
    {
        _context.MediaFiles.Remove(mediaFile);
        await _context.SaveChangesAsync();
    }
}

public class UploadChunkRepository : IUploadChunkRepository
{
    private readonly ApplicationDbContext _context;

    public UploadChunkRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UploadChunk> SaveAsync(UploadChunk chunk)
    {
        _context.UploadChunks.Add(chunk);
        await _context.SaveChangesAsync();
        return chunk;
    }

    public async Task<UploadChunk?> GetByIdAsync(Guid id)
    {
        return await _context.UploadChunks.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<UploadChunk>> GetChunksByMediaFileAsync(Guid mediaFileId)
    {
        return await _context.UploadChunks
            .Where(c => c.MediaFileId == mediaFileId)
            .OrderBy(c => c.ChunkNumber)
            .ToListAsync();
    }

    public async Task<UploadChunk?> GetChunkAsync(Guid mediaFileId, int chunkNumber)
    {
        return await _context.UploadChunks
            .FirstOrDefaultAsync(c => c.MediaFileId == mediaFileId && c.ChunkNumber == chunkNumber);
    }

    public async Task UpdateAsync(UploadChunk chunk)
    {
        _context.UploadChunks.Update(chunk);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteChunkAsync(Guid id)
    {
        var chunk = await GetByIdAsync(id);
        if (chunk != null)
        {
            _context.UploadChunks.Remove(chunk);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllForFileAsync(Guid mediaFileId)
    {
        var chunks = await _context.UploadChunks.Where(c => c.MediaFileId == mediaFileId).ToListAsync();
        if (chunks.Count == 0) return;
        _context.UploadChunks.RemoveRange(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetChunkCountAsync(Guid mediaFileId)
    {
        return await _context.UploadChunks
            .CountAsync(c => c.MediaFileId == mediaFileId);
    }
}
