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

    public async Task<int> GetChunkCountAsync(Guid mediaFileId)
    {
        return await _context.UploadChunks
            .CountAsync(c => c.MediaFileId == mediaFileId);
    }
}
