namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Dto;
    using Infrastructure.Security;
    using Infrastructure.Storage;
    using System.Security.Cryptography;

public interface IMediaUploadService
{
    Task<MediaFileDto> InitializeUploadAsync(string fileName, string contentType, long totalSizeBytes, int fileType, Guid shopId, Guid userId);
    Task<ChunkUploadResponseDto> UploadChunkAsync(Guid mediaFileId, int chunkNumber, Stream data, string checksumMD5, Guid shopId);
    Task<MediaFileDto> CompleteUploadAsync(Guid mediaFileId, Guid shopId);
    Task<MediaFileDto?> GetMediaFileAsync(Guid mediaFileId, Guid shopId);
    Task<MediaUrlDto> GetMediaUrlAsync(Guid mediaFileId, Guid shopId, int? expirationMinutes = null);

    /// <summary>Opens a file for a signed link. Returns null when the link is invalid, expired or the file is gone.</summary>
    Task<MediaDownload?> OpenDownloadAsync(Guid mediaFileId, long expiresUnixSeconds, string? signature);
}

public sealed class MediaDownload : IAsyncDisposable
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public class MediaUploadService : IMediaUploadService
{
    /// <summary>Size of each piece the browser uploads. Small enough for the web server's request limit.</summary>
    public const long ChunkSizeBytes = 8 * 1024 * 1024;

    private const long MaxFileSize = 536870912;  // 500MB
    private const long MaxChunkSize = 104857600;  // 100MB
    private const int MaxUrlExpirationMinutes = 43200;  // 30 days

    // Only formats a browser shows as an image or video. Active content such as SVG or HTML is refused,
    // because files are served from the API's own address.
    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        ["image/png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        ["image/jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/gif"] = new[] { "GIF87a"u8.ToArray(), "GIF89a"u8.ToArray() },
        ["image/webp"] = new[] { "RIFF"u8.ToArray() },
        ["video/mp4"] = new[] { Array.Empty<byte>() },   // "ftyp" at offset 4, checked separately
        ["video/webm"] = new[] { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } },
    };

    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IUploadChunkRepository _chunkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageProvider _storage;
    private readonly IMediaUrlSigner _signer;

    public MediaUploadService(
        IMediaFileRepository mediaFileRepository,
        IUploadChunkRepository chunkRepository,
        IUnitOfWork unitOfWork,
        IStorageProvider storage,
        IMediaUrlSigner signer)
    {
        _mediaFileRepository = mediaFileRepository;
        _chunkRepository = chunkRepository;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _signer = signer;
    }

    public static string NormalizeContentType(string contentType) =>
        contentType.Split(';')[0].Trim().ToLowerInvariant();

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

        var type = NormalizeContentType(contentType);
        if (!Signatures.ContainsKey(type))
            throw new ArgumentException("Only PNG, JPEG, GIF, WebP images and MP4, WebM videos can be uploaded");
        if ((fileType == 2) != type.StartsWith("video/"))
            throw new ArgumentException("The file type does not match its content type");

        var mediaFile = new MediaFile(shopId, fileName, type, totalSizeBytes, fileType, userId);
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
        if (data.Length == 0)
            throw new ArgumentException("Chunk is empty");
        if (data.Length > MaxChunkSize)
            throw new ArgumentException($"Chunk size must be 0-{MaxChunkSize / 1024 / 1024}MB");
        if (string.IsNullOrWhiteSpace(checksumMD5) || checksumMD5.Length != 32)
            throw new ArgumentException("Invalid MD5 checksum format");

        // Verify checksum
        data.Seek(0, SeekOrigin.Begin);
        using var md5 = MD5.Create();
        var actualChecksum = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        if (actualChecksum != checksumMD5.ToLowerInvariant())
            throw new InvalidOperationException("Checksum mismatch - data integrity failed");

        // A chunk sent again replaces the earlier one
        var existing = await _chunkRepository.GetChunkAsync(mediaFileId, chunkNumber);
        if (existing != null)
            await _chunkRepository.DeleteChunkAsync(existing.Id);

        var chunk = new UploadChunk(mediaFileId, chunkNumber, data.Length, checksumMD5.ToLowerInvariant());

        data.Seek(0, SeekOrigin.Begin);
        await _storage.SaveChunkAsync(chunk.StoragePath, data);

        await _chunkRepository.SaveAsync(chunk);
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

        var chunks = (await _chunkRepository.GetChunksByMediaFileAsync(mediaFileId))
            .OrderBy(c => c.ChunkNumber)
            .ToList();
        if (chunks.Count == 0)
            throw new InvalidOperationException("No chunks uploaded");

        // Verify all chunks are uploaded
        var uploadedChunks = chunks.Where(c => c.Status >= (int)UploadChunkStatus.Uploaded).ToList();
        if (uploadedChunks.Count != chunks.Count)
            throw new InvalidOperationException("Not all chunks have been uploaded");

        // The pieces must be 0..n-1 with no gaps and add up to the size announced at the start
        if (chunks.Select(c => c.ChunkNumber).SequenceEqual(Enumerable.Range(0, chunks.Count)) == false)
            throw new InvalidOperationException("Chunks are missing from the upload");
        if (chunks.Sum(c => c.SizeBytes) != mediaFile.SizeBytes)
            throw new InvalidOperationException("The uploaded size does not match the announced size");

        await AssembleAsync(mediaFile, chunks);

        if (!await HasExpectedSignatureAsync(mediaFile))
        {
            await _storage.DeleteChunkAsync(mediaFile.StoragePath);
            foreach (var chunk in chunks)
                await _storage.DeleteChunkAsync(chunk.StoragePath);
            mediaFile.MarkAsFailed();
            await _mediaFileRepository.UpdateAsync(mediaFile);
            await _unitOfWork.CommitAsync();
            throw new InvalidOperationException("The file content does not match its type");
        }

        // Mark chunks as verified
        foreach (var chunk in chunks)
        {
            if (chunk.Status == (int)UploadChunkStatus.Uploaded)
            {
                chunk.MarkAsVerified();
                await _chunkRepository.UpdateAsync(chunk);
            }
            await _storage.DeleteChunkAsync(chunk.StoragePath);
        }

        // Mark file as active
        mediaFile.MarkAsActive();
        await _mediaFileRepository.UpdateAsync(mediaFile);
        await _unitOfWork.CommitAsync();

        return MapToDto(mediaFile);
    }

    private async Task AssembleAsync(MediaFile mediaFile, List<UploadChunk> chunks)
    {
        await _storage.DeleteChunkAsync(mediaFile.StoragePath); // start clean if an earlier attempt failed halfway
        try
        {
            foreach (var chunk in chunks)
            {
                await using var piece = await _storage.GetChunkAsync(chunk.StoragePath);
                await _storage.AppendAsync(mediaFile.StoragePath, piece);
            }
        }
        catch
        {
            await _storage.DeleteChunkAsync(mediaFile.StoragePath);
            throw;
        }
    }

    private async Task<bool> HasExpectedSignatureAsync(MediaFile mediaFile)
    {
        if (!Signatures.TryGetValue(NormalizeContentType(mediaFile.ContentType), out var accepted))
            return false;

        var header = new byte[16];
        int read;
        await using (var stream = await _storage.OpenReadAsync(mediaFile.StoragePath))
            read = await stream.ReadAsync(header.AsMemory(0, header.Length));

        var type = NormalizeContentType(mediaFile.ContentType);
        if (type == "video/mp4")
            return read >= 12 && header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p';

        if (type == "image/webp")
            return read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';

        return accepted.Any(signature => read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));
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
        var signature = _signer.Sign(mediaFileId, expiresAt);
        var expires = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var url = $"/api/media/{mediaFileId}/download?expires={expires}&sig={signature}";

        return new MediaUrlDto
        {
            MediaFileId = mediaFileId,
            Url = url,
            ExpiresAt = expiresAt
        };
    }

    public async Task<MediaDownload?> OpenDownloadAsync(Guid mediaFileId, long expiresUnixSeconds, string? signature)
    {
        if (!_signer.Verify(mediaFileId, expiresUnixSeconds, signature, DateTime.UtcNow))
            return null;

        var mediaFile = await _mediaFileRepository.GetByIdUnscopedAsync(mediaFileId);
        if (mediaFile == null || mediaFile.Status != (int)MediaFileStatus.Active)
            return null;

        try
        {
            return new MediaDownload
            {
                Content = await _storage.OpenReadAsync(mediaFile.StoragePath),
                ContentType = NormalizeContentType(mediaFile.ContentType),
                FileName = mediaFile.FileName
            };
        }
        catch (FileNotFoundException)
        {
            return null;
        }
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
