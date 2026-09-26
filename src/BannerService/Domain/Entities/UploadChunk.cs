namespace BannerService.Domain.Entities;

public class UploadChunk
{
    public Guid Id { get; set; }
    public Guid MediaFileId { get; set; }
    public int ChunkNumber { get; set; }
    public long SizeBytes { get; set; }
    public string ChecksumMD5 { get; set; } = string.Empty;
    public int Status { get; set; }  // 1=Pending, 2=Uploaded, 3=Verified, 4=Failed
    public DateTime? UploadedAt { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public UploadChunk() { }

    public UploadChunk(Guid mediaFileId, int chunkNumber, long sizeBytes, string checksumMD5)
    {
        if (chunkNumber < 0 || chunkNumber > 9999)
            throw new ArgumentException("ChunkNumber must be 0-9999");
        if (sizeBytes < 0 || sizeBytes > 104857600)  // 100MB max per chunk
            throw new ArgumentException("SizeBytes must be 0-100MB");
        if (string.IsNullOrWhiteSpace(checksumMD5) || checksumMD5.Length != 32)
            throw new ArgumentException("ChecksumMD5 must be 32 hex characters");

        Id = Guid.NewGuid();
        MediaFileId = mediaFileId;
        ChunkNumber = chunkNumber;
        SizeBytes = sizeBytes;
        ChecksumMD5 = checksumMD5;
        Status = 1;  // Pending
        StoragePath = $"uploads/{mediaFileId}/chunk_{chunkNumber}";
    }

    public void MarkAsUploaded()
    {
        if (Status != 1)
            throw new InvalidOperationException("Can only upload Pending chunks");
        Status = 2;
        UploadedAt = DateTime.UtcNow;
    }

    public void MarkAsVerified()
    {
        if (Status != 2)
            throw new InvalidOperationException("Can only verify Uploaded chunks");
        Status = 3;
    }

    public void MarkAsFailed()
    {
        Status = 4;
    }
}

public enum UploadChunkStatus
{
    Pending = 1,
    Uploaded = 2,
    Verified = 3,
    Failed = 4
}
