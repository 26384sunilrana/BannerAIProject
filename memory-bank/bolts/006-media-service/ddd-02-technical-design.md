# DDD-02: Technical Design — Media Service

**Status**: Stage 2 of 5 (Technical Design)  
**Bolt**: 006-media-service  
**Created**: 2026-09-26  

---

## Architecture Overview

Media Service is a **separate microservice** from Banner Service. It handles all media upload, storage, and retrieval operations.

```
┌─────────────────────────────────────┐
│      Media Service (NEW)             │
│                                      │
│  Presentation Layer                  │
│  - MediaController                   │
│  - Upload endpoints                  │
│                                      │
│  Application Layer                   │
│  - MediaUploadService                │
│  - MetadataExtractionService        │
│                                      │
│  Domain Layer                        │
│  - MediaFile aggregate               │
│  - UploadChunk entity                │
│  - VideoMetadata value object        │
│                                      │
│  Infrastructure Layer                │
│  - File storage (local/Azure)        │
│  - FFprobe for metadata              │
│  - MediaFile & Chunk repositories    │
└─────────────────────────────────────┘
         ↓
┌─────────────────────────────────────┐
│      Banner Service (Consumer)       │
│  References media files via URL      │
└─────────────────────────────────────┘
```

---

## Project Structure

```
src/MediaService/
├── Domain/
│   ├── Entities/
│   │   ├── MediaFile.cs
│   │   └── UploadChunk.cs
│   ├── ValueObjects/
│   │   ├── VideoMetadata.cs
│   │   └── MediaUrl.cs
│   ├── Interfaces/
│   │   ├── IMediaFileRepository.cs
│   │   └── IUploadChunkRepository.cs
│   └── Services/
│       ├── MediaUploadService.cs
│       └── MetadataExtractionService.cs
├── Application/
│   ├── Services/
│   │   ├── MediaUploadApplicationService.cs
│   │   └── MetadataService.cs
│   └── Dto/
│       ├── MediaFileDto.cs
│       ├── UploadResponseDto.cs
│       └── MediaUrlDto.cs
├── Infrastructure/
│   ├── Data/
│   │   ├── MediaDbContext.cs
│   │   └── Migrations/
│   ├── Storage/
│   │   ├── IStorageProvider.cs
│   │   ├── LocalStorageProvider.cs
│   │   └── AzureBlobStorageProvider.cs
│   └── Repositories/
│       ├── MediaFileRepository.cs
│       └── UploadChunkRepository.cs
├── Presentation/
│   ├── Controllers/
│   │   └── MediaController.cs
│   └── Middleware/
│       ├── ShopContextMiddleware.cs
│       └── ExceptionHandlingMiddleware.cs
└── Program.cs
```

---

## Database Schema

### MediaFiles Table

```sql
CREATE TABLE [dbo].[MediaFiles] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [ShopId] [uniqueidentifier] NOT NULL,
    [FileName] [nvarchar](255) NOT NULL,
    [FileType] [int] NOT NULL,
    [ContentType] [nvarchar](100) NOT NULL,
    [SizeBytes] [bigint] NOT NULL,
    [StoragePath] [nvarchar](500) NOT NULL UNIQUE,
    [Status] [int] NOT NULL,
    [CreatedAt] [datetime2] NOT NULL,
    [CompletedAt] [datetime2] NULL,
    [CreatedBy] [uniqueidentifier] NOT NULL,
    [VideoMetadata] [nvarchar](max) NULL,
    
    INDEX [IX_MediaFiles_Query] ON ([ShopId], [Status], [CreatedAt] DESC)
);
```

**FileType Enum**:
- 1 = Image
- 2 = Video
- 3 = Graphics

**Status Enum**:
- 1 = Pending
- 2 = Active
- 3 = Failed
- 4 = Deleted

### UploadChunks Table

```sql
CREATE TABLE [dbo].[UploadChunks] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [MediaFileId] [uniqueidentifier] NOT NULL,
    [ChunkNumber] [int] NOT NULL,
    [SizeBytes] [bigint] NOT NULL,
    [ChecksumMD5] [nvarchar](32) NOT NULL,
    [Status] [int] NOT NULL,
    [UploadedAt] [datetime2] NULL,
    [StoragePath] [nvarchar](500) NOT NULL,
    
    CONSTRAINT [FK_UploadChunks_MediaFiles] 
        FOREIGN KEY ([MediaFileId]) REFERENCES [dbo].[MediaFiles]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [UQ_UploadChunks_Unique] 
        UNIQUE ([MediaFileId], [ChunkNumber]),
    
    INDEX [IX_UploadChunks_Query] ON ([MediaFileId], [ChunkNumber])
);
```

---

## Storage Provider Pattern

### IStorageProvider Interface

```csharp
public interface IStorageProvider
{
    Task<string> UploadChunkAsync(string path, Stream data, Guid chunkId);
    Task<Stream> DownloadAsync(string path);
    Task DeleteAsync(string path);
    Task<bool> ExistsAsync(string path);
    Task<string> GetUrlAsync(string path, TimeSpan? expiration);
}
```

### LocalStorageProvider

Stores files in project directory structure:
```
/storage/
├── uploads/
│   ├── {mediaFileId}/
│   │   ├── chunk_0
│   │   ├── chunk_1
│   │   └── ...
```

### AzureBlobStorageProvider (Future)

For production Azure deployment.

---

## API Endpoints

### Single File Upload

```
POST /api/media/upload
Content-Type: multipart/form-data

Request:
{
  file: File,
  fileType: 1  // Image
}

Response (200):
{
  id: "uuid",
  fileName: "photo.jpg",
  contentType: "image/jpeg",
  sizeBytes: 2048576,
  status: 2,  // Active
  completedAt: "2026-09-26T10:00:00Z",
  url: "https://media.service/api/media/{id}/download"
}
```

### Chunked Upload - Initialize

```
POST /api/media/chunked/initialize
Content-Type: application/json

Request:
{
  fileName: "video.mp4",
  contentType: "video/mp4",
  totalSizeBytes: 536870912,  // 500MB
  fileType: 2
}

Response (200):
{
  mediaFileId: "uuid",
  chunkSize: 104857600,  // 100MB
  totalChunks: 5
}
```

### Chunked Upload - Upload Chunk

```
PUT /api/media/{mediaFileId}/chunks/{chunkNumber}
Content-Type: application/octet-stream

Request: Binary chunk data
Headers: X-Checksum-MD5: "abc123..."

Response (200):
{
  chunkNumber: 0,
  status: 2,  // Uploaded
  uploadedAt: "2026-09-26T10:00:00Z"
}
```

### Chunked Upload - Complete

```
POST /api/media/{mediaFileId}/complete
Content-Type: application/json

Response (200):
{
  id: "uuid",
  fileName: "video.mp4",
  status: 2,  // Active
  completedAt: "2026-09-26T10:05:00Z",
  videoMetadata: {
    duration: "00:45:30",
    resolution: "1920x1080",
    frameRate: 30,
    codec: "h264",
    bitrate: 5000000
  }
}
```

### Get Media URL

```
GET /api/media/{mediaFileId}/url?expirationMinutes=60

Response (200):
{
  url: "https://storage.service/uploads/{mediaFileId}/file?token=...",
  expiresAt: "2026-09-26T11:00:00Z"
}
```

### Download Media

```
GET /api/media/{mediaFileId}/download

Response (200):
Binary file content
Content-Type: video/mp4
Content-Disposition: attachment; filename="video.mp4"
```

---

## Services Implementation

### MediaUploadApplicationService

```csharp
public class MediaUploadApplicationService
{
    public async Task<MediaFileDto> InitializeUploadAsync(
        string fileName, string contentType, long totalSizeBytes, 
        int fileType, Guid shopId)
    {
        // Validate parameters
        // Create MediaFile entity
        // Generate storage path
        // Persist to repository
        // Return DTO
    }
    
    public async Task UploadChunkAsync(
        Guid mediaFileId, int chunkNumber, Stream data, 
        string checksumMD5, Guid shopId)
    {
        // Verify media file exists
        // Validate chunk number
        // Calculate actual checksum
        // Verify checksum matches
        // Upload via storage provider
        // Create UploadChunk entity
        // Persist chunk
    }
    
    public async Task<MediaFileDto> CompleteUploadAsync(
        Guid mediaFileId, Guid shopId)
    {
        // Find media file
        // Verify all chunks uploaded
        // Reconstruct file from chunks
        // Set status to Active
        // Extract metadata if video
        // Return updated DTO
    }
    
    public async Task<MediaUrlDto> GetMediaUrlAsync(
        Guid mediaFileId, Guid shopId, int expirationMinutes)
    {
        // Find media file
        // Verify Active status
        // Generate token/signature
        // Get URL from storage provider
        // Return URL DTO with expiration
    }
}
```

### MetadataExtractionService

```csharp
public class MetadataExtractionService
{
    public async Task<VideoMetadata> ExtractVideoMetadataAsync(string filePath)
    {
        // Use FFprobe to analyze file
        // Extract: duration, resolution, codecs, bitrate
        // Return VideoMetadata object
        // Return null if not a video
    }
}
```

---

## DTOs

```csharp
public class MediaFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public int FileType { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public VideoMetadataDto? VideoMetadata { get; set; }
}

public class VideoMetadataDto
{
    public string Duration { get; set; }
    public string Resolution { get; set; }
    public decimal FrameRate { get; set; }
    public string Codec { get; set; }
    public long Bitrate { get; set; }
}

public class MediaUrlDto
{
    public Guid MediaFileId { get; set; }
    public string Url { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class InitializeChunkedUploadRequestDto
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long TotalSizeBytes { get; set; }
    public int FileType { get; set; }
}

public class InitializeChunkedUploadResponseDto
{
    public Guid MediaFileId { get; set; }
    public long ChunkSizeBytes { get; set; }
    public int TotalChunks { get; set; }
}
```

---

## Configuration

### appsettings.json

```json
{
  "MediaService": {
    "StorageProvider": "Local",  // or "AzureBlob"
    "LocalStoragePath": "./storage",
    "MaxFileSize": 536870912,  // 500MB
    "MaxChunkSize": 104857600,  // 100MB
    "ChunkingThreshold": 10485760,  // 10MB (auto-chunk if > 10MB)
    "UrlExpirationMinutes": 43200,  // 30 days default
    "AllowedMimeTypes": [
      "image/jpeg",
      "image/png",
      "image/webp",
      "video/mp4",
      "video/webm",
      "video/quicktime"
    ],
    "FFprobePath": "ffprobe"  // or full path to ffprobe executable
  }
}
```

---

## Integration Points

### With Banner Service

Banner Service calls Media Service to:
1. Generate media URLs for components
2. Validate media files exist
3. Get metadata for rendering hints

Banner Service **does not**:
- Store media files
- Handle uploads
- Extract metadata

---

## Authentication & Security

### Multi-tenant Isolation
- All queries filter by ShopId
- ShopId extracted from JWT claim
- Enforced at repository layer

### File Upload Security
- Validate MIME type
- Validate file size
- Validate content (file signature)
- Sanitize filename

### URL Security
- Time-limited tokens
- Signature-based validation
- Shop-specific tokens
- Rate limiting on downloads

---

## Error Handling

**HTTP Status Codes**:
- 201: Upload successful
- 400: Invalid parameters
- 401: Unauthorized
- 404: Media file not found
- 409: Checksum mismatch
- 413: File too large
- 500: Server error

---

## Next: ADR Analysis (Stage 3)

Key decisions to document:
1. Storage strategy (local vs cloud)
2. Chunking threshold and size
3. Metadata extraction (sync vs async)
4. URL expiration policy
5. File naming strategy
