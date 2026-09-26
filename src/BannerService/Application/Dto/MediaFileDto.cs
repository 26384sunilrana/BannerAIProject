namespace BannerService.Application.Dto;

public class MediaFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int FileType { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class InitializeUploadRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public int FileType { get; set; }
}

public class InitializeUploadResponseDto
{
    public Guid MediaFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public long ChunkSizeBytes { get; set; } = 104857600;  // 100MB
    public int TotalChunks { get; set; }
}

public class ChunkUploadResponseDto
{
    public int ChunkNumber { get; set; }
    public int Status { get; set; }
    public DateTime? UploadedAt { get; set; }
}

public class MediaUrlDto
{
    public Guid MediaFileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
