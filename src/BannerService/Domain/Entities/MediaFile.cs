namespace BannerService.Domain.Entities;

using ValueObjects;

public class MediaFile
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int FileType { get; set; }  // 1=Image, 2=Video, 3=Graphics
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public int Status { get; set; }  // 1=Pending, 2=Active, 3=Failed, 4=Deleted
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? VideoMetadataJson { get; set; }

    /// <summary>Read from the file when the upload completes; empty when the header could not be understood.</summary>
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }

    /// <summary>When the file was removed. The record stays for a while as a tombstone, then the clean-up job drops it.</summary>
    public DateTime? DeletedAt { get; set; }

    public MediaFile() { }

    public MediaFile(Guid shopId, string fileName, string contentType, long sizeBytes, int fileType, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            throw new ArgumentException("FileName must be 1-255 characters");
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("ContentType required");
        if (sizeBytes < 0 || sizeBytes > 536870912)  // 500MB max
            throw new ArgumentException("SizeBytes must be 0-500MB");
        if (fileType < 1 || fileType > 3)
            throw new ArgumentException("FileType must be 1-3");

        Id = Guid.NewGuid();
        ShopId = shopId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        FileType = fileType;
        CreatedBy = createdBy;
        Status = 1;  // Pending
        CreatedAt = DateTime.UtcNow;
        StoragePath = $"uploads/{Id}/{Id}.bin";
    }

    public void MarkAsActive()
    {
        if (Status != 1)
            throw new InvalidOperationException("Can only mark Pending files as Active");
        Status = 2;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        if (Status != 1)
            throw new InvalidOperationException("Can only fail Pending uploads");
        Status = 3;
    }

    public void SetDimensions(int? width, int? height, double? durationSeconds)
    {
        Width = width;
        Height = height;
        DurationSeconds = durationSeconds;
    }

    /// <summary>Marks the file as removed. Works from any state; the stored bytes are deleted by the caller.</summary>
    public void MarkAsDeleted()
    {
        Status = (int)MediaFileStatus.Deleted;
        DeletedAt = DateTime.UtcNow;
    }

    public void SetVideoMetadata(string metadataJson)
    {
        if (FileType != 2)
            throw new InvalidOperationException("Only videos can have metadata");
        VideoMetadataJson = metadataJson;
    }
}

public enum MediaFileType
{
    Image = 1,
    Video = 2,
    Graphics = 3
}

public enum MediaFileStatus
{
    Pending = 1,
    Active = 2,
    Failed = 3,
    Deleted = 4
}
