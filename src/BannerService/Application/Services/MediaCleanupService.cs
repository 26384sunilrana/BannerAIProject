namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Infrastructure.Storage;

    public record MediaCleanupReport(int AbandonedRemoved, int RecordsDropped);

    /// <summary>
    /// Tidies the media store. Uploads that were started and never finished (or failed) lose their bytes and pieces; files a shop
    /// deleted leave a small record for a while, and that record is dropped here. Every step can be repeated, so several servers
    /// may run it at once.
    /// </summary>
    public class MediaCleanupService
    {
        private readonly IMediaFileRepository _files;
        private readonly IUploadChunkRepository _chunks;
        private readonly IStorageProvider _storage;
        private readonly TimeSpan _abandonedAfter;
        private readonly TimeSpan _keepRecordsFor;
        private readonly ILogger<MediaCleanupService> _logger;

        public MediaCleanupService(
            IMediaFileRepository files,
            IUploadChunkRepository chunks,
            IStorageProvider storage,
            IConfiguration configuration,
            ILogger<MediaCleanupService> logger)
        {
            _files = files;
            _chunks = chunks;
            _storage = storage;
            _abandonedAfter = TimeSpan.FromHours(Math.Max(1, configuration.GetValue("Media:Cleanup:AbandonedUploadHours", 24)));
            _keepRecordsFor = TimeSpan.FromDays(Math.Max(1, configuration.GetValue("Media:Cleanup:KeepDeletedRecordsDays", 30)));
            _logger = logger;
        }

        public async Task<MediaCleanupReport> RunAsync(DateTime now)
        {
            var removed = 0;
            foreach (var file in await _files.GetAbandonedAsync(now - _abandonedAfter))
            {
                await RemoveBytesAsync(file);
                file.MarkAsDeleted();
                await _files.UpdateAsync(file);
                removed++;
            }

            var dropped = 0;
            foreach (var file in await _files.GetTombstonesAsync(now - _keepRecordsFor))
            {
                try
                {
                    await RemoveBytesAsync(file); // normally already gone; makes sure
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Stored bytes of {Id} are still in use; trying again next run", file.Id);
                    continue;
                }

                await _files.RemoveAsync(file);
                dropped++;
            }

            if (removed > 0 || dropped > 0)
                _logger.LogInformation("Media clean-up: {Removed} unfinished uploads removed, {Dropped} old records dropped", removed, dropped);
            return new MediaCleanupReport(removed, dropped);
        }

        private async Task RemoveBytesAsync(MediaFile file)
        {
            foreach (var chunk in await _chunks.GetChunksByMediaFileAsync(file.Id))
                await _storage.DeleteChunkAsync(chunk.StoragePath);
            await _chunks.DeleteAllForFileAsync(file.Id);
            await _storage.DeleteChunkAsync(file.StoragePath);
        }
    }
}
