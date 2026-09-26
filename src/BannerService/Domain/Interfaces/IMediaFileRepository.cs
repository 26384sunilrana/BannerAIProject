namespace BannerService.Domain.Interfaces;

using Entities;

public interface IMediaFileRepository
{
    Task<MediaFile> SaveAsync(MediaFile mediaFile);
    Task<MediaFile?> GetByIdAsync(Guid id, Guid shopId);
    Task<List<MediaFile>> GetByShopAsync(Guid shopId);
    Task UpdateAsync(MediaFile mediaFile);
    Task DeleteAsync(Guid id, Guid shopId);
}

public interface IUploadChunkRepository
{
    Task<UploadChunk> SaveAsync(UploadChunk chunk);
    Task<UploadChunk?> GetByIdAsync(Guid id);
    Task<List<UploadChunk>> GetChunksByMediaFileAsync(Guid mediaFileId);
    Task<UploadChunk?> GetChunkAsync(Guid mediaFileId, int chunkNumber);
    Task UpdateAsync(UploadChunk chunk);
    Task DeleteChunkAsync(Guid id);
    Task<int> GetChunkCountAsync(Guid mediaFileId);
}
