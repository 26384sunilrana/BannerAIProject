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
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
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
    public long ChunkSizeBytes { get; set; }
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

/// <summary>One file in the media library: what it is, how big, a link a browser can load, and whether a banner uses it.</summary>
public class MediaLibraryItemDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int FileType { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool InUse { get; set; }
}

public class MediaLibraryPageDto
{
    public List<MediaLibraryItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>What the shop has stored. LimitBytes is the plan's storage allowance, or null when the shop has no plan.</summary>
public class MediaUsageDto
{
    public long UsedBytes { get; set; }
    public int FileCount { get; set; }
    public int ImageCount { get; set; }
    public int VideoCount { get; set; }
    public long? LimitBytes { get; set; }
}
