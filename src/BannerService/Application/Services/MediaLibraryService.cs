namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Dto;
    using Infrastructure.Security;
    using Infrastructure.Storage;

    public enum MediaDeleteOutcome { Deleted, NotFound, InUse }

    public record MediaDeleteResult(MediaDeleteOutcome Outcome, string Message, IReadOnlyList<string> Banners)
    {
        public static MediaDeleteResult Deleted() => new(MediaDeleteOutcome.Deleted, "The file was deleted.", Array.Empty<string>());
        public static MediaDeleteResult NotFound() => new(MediaDeleteOutcome.NotFound, "Media file not found.", Array.Empty<string>());
    }

    /// <summary>A shop's media library: browsing what it has uploaded, what it is using up, and removing what no banner needs.</summary>
    public class MediaLibraryService
    {
        public const int MaxPageSize = 100;
        private const int LinkMinutes = 240;
        private const long BytesPerGb = 1024L * 1024 * 1024;

        private readonly IMediaFileRepository _files;
        private readonly IUploadChunkRepository _chunks;
        private readonly ISubscriptionRepository _subscriptions;
        private readonly IStorageProvider _storage;
        private readonly IMediaUrlSigner _signer;
        private readonly ILogger<MediaLibraryService> _logger;

        public MediaLibraryService(
            IMediaFileRepository files,
            IUploadChunkRepository chunks,
            ISubscriptionRepository subscriptions,
            IStorageProvider storage,
            IMediaUrlSigner signer,
            ILogger<MediaLibraryService> logger)
        {
            _files = files;
            _chunks = chunks;
            _subscriptions = subscriptions;
            _storage = storage;
            _signer = signer;
            _logger = logger;
        }

        public async Task<MediaLibraryPageDto> ListAsync(Guid shopId, int? fileType, string? search, int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var (files, total) = await _files.GetActivePageAsync(shopId, fileType, search, page, pageSize);
            var expires = DateTime.UtcNow.AddMinutes(LinkMinutes);

            var items = new List<MediaLibraryItemDto>(files.Count);
            foreach (var file in files)
            {
                items.Add(new MediaLibraryItemDto
                {
                    Id = file.Id,
                    FileName = file.FileName,
                    FileType = file.FileType,
                    ContentType = file.ContentType,
                    SizeBytes = file.SizeBytes,
                    Width = file.Width,
                    Height = file.Height,
                    DurationSeconds = file.DurationSeconds,
                    CreatedAt = file.CreatedAt,
                    Url = SignedUrl(file.Id, expires),
                    InUse = (await _files.FindUsingBannersAsync(shopId, file.Id)).Count > 0,
                });
            }

            return new MediaLibraryPageDto { Items = items, Total = total, Page = page, PageSize = pageSize };
        }

        public async Task<MediaUsageDto> GetUsageAsync(Guid shopId)
        {
            var usage = await _files.GetUsageAsync(shopId);
            var subscription = await _subscriptions.GetByShopIdAsync(shopId);
            var gigabytes = subscription?.Plan?.Features?.MaxStorageGB;

            return new MediaUsageDto
            {
                UsedBytes = usage.Bytes,
                FileCount = usage.Files,
                ImageCount = usage.Images,
                VideoCount = usage.Videos,
                LimitBytes = gigabytes is > 0 ? gigabytes.Value * BytesPerGb : null,
            };
        }

        /// <summary>
        /// Removes a file the shop no longer wants. A file that a banner shows (or an earlier version of a banner still holds,
        /// which a restore would bring back) is refused, and the banners are named so they can be changed first.
        /// </summary>
        public async Task<MediaDeleteResult> DeleteAsync(Guid shopId, Guid mediaFileId)
        {
            var file = await _files.GetByIdAsync(mediaFileId, shopId);
            if (file == null || file.Status == (int)MediaFileStatus.Deleted)
                return MediaDeleteResult.NotFound();

            var banners = await _files.FindUsingBannersAsync(shopId, mediaFileId);
            if (banners.Count > 0)
            {
                var names = string.Join(", ", banners.Take(5)) + (banners.Count > 5 ? $" and {banners.Count - 5} more" : string.Empty);
                return new MediaDeleteResult(
                    MediaDeleteOutcome.InUse,
                    $"{file.FileName} is used by {names}. Take it out of those banners (and their earlier versions) first.",
                    banners);
            }

            await RemoveStoredBytesAsync(file);
            file.MarkAsDeleted();
            await _files.UpdateAsync(file);
            _logger.LogInformation("Media file {Id} deleted for shop {ShopId}", mediaFileId, shopId);
            return MediaDeleteResult.Deleted();
        }

        /// <summary>Deletes the stored file and any pieces of an unfinished upload, and forgets the pieces.</summary>
        internal async Task RemoveStoredBytesAsync(MediaFile file)
        {
            foreach (var chunk in await _chunks.GetChunksByMediaFileAsync(file.Id))
                await _storage.DeleteChunkAsync(chunk.StoragePath);
            await _chunks.DeleteAllForFileAsync(file.Id);
            try
            {
                await _storage.DeleteChunkAsync(file.StoragePath);
            }
            catch (IOException ex)
            {
                // a browser may still be streaming the file; the record is marked deleted anyway and the clean-up removes the bytes later
                _logger.LogWarning(ex, "Could not delete the stored bytes of {Id} yet", file.Id);
            }
        }

        private string SignedUrl(Guid id, DateTime expiresAt)
        {
            var signature = _signer.Sign(id, expiresAt);
            var expires = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
            return $"/api/media/{id}/download?expires={expires}&sig={signature}";
        }
    }
}
