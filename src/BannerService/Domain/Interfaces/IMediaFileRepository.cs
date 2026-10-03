namespace BannerService.Domain.Interfaces;

using Entities;

public interface IMediaFileRepository
{
    Task<MediaFile> SaveAsync(MediaFile mediaFile);
    Task<MediaFile?> GetByIdAsync(Guid id, Guid shopId);
    /// <summary>Only for serving a signed download link, where the signature proves access.</summary>
    Task<MediaFile?> GetByIdUnscopedAsync(Guid id);
    Task<List<MediaFile>> GetByShopAsync(Guid shopId);
    Task UpdateAsync(MediaFile mediaFile);
    Task DeleteAsync(Guid id, Guid shopId);

    /// <summary>The shop's usable files, newest first. fileType 1 = image, 2 = video; search matches part of the file name.</summary>
    Task<(List<MediaFile> items, int total)> GetActivePageAsync(Guid shopId, int? fileType, string? search, int page, int pageSize);

    /// <summary>What the shop's usable files add up to.</summary>
    Task<MediaUsage> GetUsageAsync(Guid shopId);

    /// <summary>Bytes taken by usable files plus uploads still in progress, which already hold their space.</summary>
    Task<long> GetReservedBytesAsync(Guid shopId);

    /// <summary>Names of the shop's banners that show the file now, or kept it in an earlier version. Empty when nothing uses it.</summary>
    Task<List<string>> FindUsingBannersAsync(Guid shopId, Guid mediaFileId);

    /// <summary>Uploads that never finished (pending longer than the limit) and uploads that failed.</summary>
    Task<List<MediaFile>> GetAbandonedAsync(DateTime pendingBefore);

    /// <summary>Removed files whose record can now be dropped.</summary>
    Task<List<MediaFile>> GetTombstonesAsync(DateTime deletedBefore);

    Task RemoveAsync(MediaFile mediaFile);
}

public record MediaUsage(long Bytes, int Files, int Images, int Videos);

public interface IUploadChunkRepository
{
    Task<UploadChunk> SaveAsync(UploadChunk chunk);
    Task<UploadChunk?> GetByIdAsync(Guid id);
    Task<List<UploadChunk>> GetChunksByMediaFileAsync(Guid mediaFileId);
    Task<UploadChunk?> GetChunkAsync(Guid mediaFileId, int chunkNumber);
    Task UpdateAsync(UploadChunk chunk);
    Task DeleteChunkAsync(Guid id);
    Task<int> GetChunkCountAsync(Guid mediaFileId);
    Task DeleteAllForFileAsync(Guid mediaFileId);
}
