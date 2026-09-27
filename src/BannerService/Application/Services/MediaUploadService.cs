namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;
    using DTOs;
    using System.Security.Cryptography;

public interface IMediaUploadService
{
    Task<MediaFileDto> InitializeUploadAsync(string fileName, string contentType, long totalSizeBytes, int fileType, Guid shopId, Guid userId);
    Task<ChunkUploadResponseDto> UploadChunkAsync(Guid mediaFileId, int chunkNumber, Stream data, string checksumMD5, Guid shopId);
    Task<MediaFileDto> CompleteUploadAsync(Guid mediaFileId, Guid shopId);
    Task<MediaFileDto?> GetMediaFileAsync(Guid mediaFileId, Guid shopId);
    Task<MediaUrlDto> GetMediaUrlAsync(Guid mediaFileId, Guid shopId, int? expirationMinutes = null);
}

public class MediaUploadService : IMediaUploadService
{
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IUploadChunkRepository _chunkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const long MaxFileSize = 536870912;  // 500MB
    private const long MaxChunkSize = 104857600;  // 100MB
    private const int MaxUrlExpirationMinutes = 43200;  // 30 days

    public MediaUploadService(
        IMediaFileRepository mediaFileRepository,
        IUploadChunkRepository chunkRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaFileRepository = mediaFileRepository;
        _chunkRepository = chunkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MediaFileDto> InitializeUploadAsync(
        string fileName, string contentType, long totalSizeBytes, int fileType, Guid shopId, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            throw new ArgumentException("FileName must be 1-255 characters");
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("ContentType required");
        if (totalSizeBytes < 0 || totalSizeBytes > MaxFileSize)
            throw new ArgumentException($"File size must be 0-{MaxFileSize / 1024 / 1024}MB");
        if (fileType < 1 || fileType > 3)
            throw new ArgumentException("FileType must be 1-3");

        var mediaFile = new MediaFile(shopId, fileName, contentType, totalSizeBytes, fileType, userId);
        await _mediaFileRepository.SaveAsync(mediaFile);
        await _unitOfWork.CommitAsync();

        return MapToDto(mediaFile);
    }

    public async Task<ChunkUploadResponseDto> UploadChunkAsync(
        Guid mediaFileId, int chunkNumber, Stream data, string checksumMD5, Guid shopId)
    {
        var mediaFile = await _mediaFileRepository.GetByIdAsync(mediaFileId, shopId);
        if (mediaFile == null)
            throw new KeyNotFoundException("Media file not found");
        if (mediaFile.Status != (int)MediaFileStatus.Pending)
            throw new InvalidOperationException("Can only upload to pending files");

        if (chunkNumber < 0 || chunkNumber > 9999)
            throw new ArgumentException("ChunkNumber must be 0-9999");
        if (data.Length > MaxChunkSize)
            throw new ArgumentException($"Chunk size must be 0-{MaxChunkSize / 1024 / 1024}MB");
        if (string.IsNullOrWhiteSpace(checksumMD5) || checksumMD5.Length != 32)
            throw new ArgumentException("Invalid MD5 checksum format");

        // Verify checksum
        using var md5 = MD5.Create();
        var actualChecksum = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        if (actualChecksum != checksumMD5.ToLowerInvariant())
            throw new InvalidOperationException("Checksum mismatch - data integrity failed");

        // Create or update chunk
        var chunk = await _chunkRepository.GetChunkAsync(mediaFileId, chunkNumber);
        if (chunk == null)
        {
            chunk = new UploadChunk(mediaFileId, chunkNumber, data.Length, checksumMD5);
            await _chunkRepository.SaveAsync(chunk);
        }
        else
        {
            chunk.MarkAsFailed();
            chunk = new UploadChunk(mediaFileId, chunkNumber, data.Length, checksumMD5);
            await _chunkRepository.SaveAsync(chunk);
        }

        chunk.MarkAsUploaded();
        await _chunkRepository.UpdateAsync(chunk);
        await _unitOfWork.CommitAsync();

        return new ChunkUploadResponseDto
        {
            ChunkNumber = chunk.ChunkNumber,
            Status = chunk.Status,
            UploadedAt = chunk.UploadedAt
        };
    }

    public async Task<MediaFileDto> CompleteUploadAsync(Guid mediaFileId, Guid shopId)
    {
        var mediaFile = await _mediaFileRepository.GetByIdAsync(mediaFileId, shopId);
        if (mediaFile == null)
            throw new KeyNotFoundException("Media file not found");
        if (mediaFile.Status != (int)MediaFileStatus.Pending)
            throw new InvalidOperationException("File is not pending");

        var chunks = await _chunkRepository.GetChunksByMediaFileAsync(mediaFileId);
        if (chunks.Count == 0)
            throw new InvalidOperationException("No chunks uploaded");

        // Verify all chunks are uploaded
        var uploadedChunks = chunks.Where(c => c.Status >= (int)UploadChunkStatus.Uploaded).ToList();
        if (uploadedChunks.Count != chunks.Count)
            throw new InvalidOperationException("Not all chunks have been uploaded");

        // Mark chunks as verified
        foreach (var chunk in chunks)
        {
            if (chunk.Status == (int)UploadChunkStatus.Uploaded)
            {
                chunk.MarkAsVerified();
                await _chunkRepository.UpdateAsync(chunk);
            }
        }

        // Mark file as active
        mediaFile.MarkAsActive();
        await _mediaFileRepository.UpdateAsync(mediaFile);
        await _unitOfWork.CommitAsync();

        return MapToDto(mediaFile);
    }

    public async Task<MediaFileDto?> GetMediaFileAsync(Guid mediaFileId, Guid shopId)
    {
        var mediaFile = await _mediaFileRepository.GetByIdAsync(mediaFileId, shopId);
        if (mediaFile == null)
            return null;
        return MapToDto(mediaFile);
    }

    public async Task<MediaUrlDto> GetMediaUrlAsync(Guid mediaFileId, Guid shopId, int? expirationMinutes = null)
    {
        var mediaFile = await _mediaFileRepository.GetByIdAsync(mediaFileId, shopId);
        if (mediaFile == null)
            throw new KeyNotFoundException("Media file not found");
        if (mediaFile.Status != (int)MediaFileStatus.Active)
            throw new InvalidOperationException("File is not active");

        var expMin = expirationMinutes ?? MaxUrlExpirationMinutes;
        if (expMin < 1 || expMin > MaxUrlExpirationMinutes)
            throw new ArgumentException($"Expiration must be 1-{MaxUrlExpirationMinutes} minutes");

        var expiresAt = DateTime.UtcNow.AddMinutes(expMin);
        var token = GenerateToken(mediaFileId, expiresAt);
        var url = $"/api/media/{mediaFileId}/download?token={token}";

        return new MediaUrlDto
        {
            MediaFileId = mediaFileId,
            Url = url,
            ExpiresAt = expiresAt
        };
    }

    private string GenerateToken(Guid mediaFileId, DateTime expiresAt)
    {
        var data = $"{mediaFileId}:{expiresAt:O}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash).Substring(0, 16);
    }

    private MediaFileDto MapToDto(MediaFile mediaFile) => new()
    {
        Id = mediaFile.Id,
        FileName = mediaFile.FileName,
        FileType = mediaFile.FileType,
        ContentType = mediaFile.ContentType,
        SizeBytes = mediaFile.SizeBytes,
        Status = mediaFile.Status,
        CreatedAt = mediaFile.CreatedAt,
        CompletedAt = mediaFile.CompletedAt
    };
}
}
