# DDD-01: Domain Model — Media Service

**Status**: Stage 1 of 5 (Domain Model)  
**Bolt**: 006-media-service  
**Created**: 2026-09-26  

---

## Problem Statement

Banner components reference media files (images, videos) via URLs. Currently:
- No centralized media storage
- No chunked upload for large files
- No video metadata extraction
- No secure URL generation
- No upload resumption capability

Users need to:
1. **Upload media files** (images, videos, graphics)
2. **Handle large files** via chunked uploads with resume
3. **Extract metadata** from videos (duration, resolution, codec)
4. **Generate URLs** securely with expiration

---

## Core Domain

### Aggregate: MediaFile

Root entity representing an uploaded media file.

**Identity**: `Id` (Guid) - unique media identifier

**Core Fields**:
- `ShopId` (Guid) - multi-tenant isolation
- `FileName` (string, 1-255 chars) - original filename
- `FileType` (enum) - Image, Video, Graphics
- `ContentType` (string) - MIME type (image/jpeg, video/mp4, etc.)
- `SizeBytes` (long) - file size in bytes
- `StoragePath` (string) - internal storage location
- `Status` (enum) - Pending, Active, Failed, Deleted
- `CreatedAt` (DateTime) - upload start time
- `CompletedAt` (DateTime?) - upload completion time
- `CreatedBy` (Guid) - user who uploaded

**Validation**:
- FileName: 1-255 characters
- FileType: Image, Video, Graphics (enum)
- ContentType: Valid MIME type
- SizeBytes: 0 to 500MB (configurable)
- StoragePath: Non-empty, unique per file

**Invariants**:
- Cannot modify after Active status
- StoragePath immutable
- Status can only progress: Pending → Active or Pending → Failed

---

### Entity: UploadChunk

Represents a single chunk in a multi-part upload.

**Fields**:
- `Id` (Guid) - chunk identifier
- `MediaFileId` (Guid) - which upload this belongs to
- `ChunkNumber` (int) - sequence (0-based)
- `SizeBytes` (long) - chunk size
- `ChecksumMD5` (string) - integrity verification
- `Status` (enum) - Pending, Uploaded, Verified, Failed
- `UploadedAt` (DateTime) - when chunk was received
- `StoragePath` (string) - where chunk is stored

**Constraints**:
- ChunkNumber: 0 to 9999
- SizeBytes: 0 to 100MB per chunk
- ChecksumMD5: 32 hex characters
- Status progression: Pending → Uploaded → Verified or Failed

**Invariants**:
- Cannot delete uploaded chunk
- StoragePath immutable per chunk

---

### Value Object: VideoMetadata

Extracted metadata from video files.

**Fields**:
- `Duration` (TimeSpan) - total duration
- `Resolution` (string) - "1920x1080", "3840x2160" (4K), "7680x4320" (8K)
- `FrameRate` (decimal) - 24, 30, 60 fps
- `Codec` (string) - "h264", "h265", "vp9", etc.
- `Bitrate` (long) - bits per second
- `AudioCodec` (string) - "aac", "mp3", "opus"
- `ExtractedAt` (DateTime) - when metadata was extracted

**Constraints**:
- Duration: > 0
- Resolution: Valid format (widthxheight)
- FrameRate: > 0
- Bitrate: > 0
- All fields can be null if extraction failed

**Immutability**: Once extracted, cannot be modified

---

### Value Object: MediaUrl

Secure URL for accessing media file.

**Fields**:
- `Url` (string) - full access URL
- `ExpiresAt` (DateTime?) - expiration time (null = no expiry)
- `IsPublic` (bool) - whether accessible without auth
- `Token` (string?) - if private, access token
- `GeneratedAt` (DateTime) - when URL was generated

**Constraints**:
- Url must be valid URI
- ExpiresAt can be null (permanent)
- Token provided if IsPublic = false

**Immutability**: Immutable after generation

---

### Domain Service: MediaUploadService

Orchestrates multi-part and single uploads.

**Operations**:

#### InitializeUpload(shopId, fileName, contentType, totalSizeBytes) → MediaFile

**Purpose**: Start new upload (single or chunked).

**Process**:
1. Validate fileName, contentType, sizeBytes
2. Create MediaFile with Pending status
3. Generate unique StoragePath
4. Return MediaFile with Id

**Constraints**:
- FileName: 1-255 chars
- ContentType: Valid MIME type
- SizeBytes: 0-500MB
- ShopId: Required

**Errors**:
- Throws if parameters invalid
- Throws if file too large

---

#### UploadChunk(mediaFileId, chunkNumber, data, checksum, shopId) → UploadChunk

**Purpose**: Receive and validate single chunk.

**Process**:
1. Find MediaFile by Id and ShopId
2. Verify chunk number is in range
3. Validate data size (max 100MB)
4. Verify MD5 checksum
5. Create and persist UploadChunk
6. Return chunk

**Constraints**:
- MediaFile must exist and be Pending
- Chunk size: 0-100MB
- ChecksumMD5: must match data
- ChunkNumber: 0-9999

**Errors**:
- Throws if file not found
- Throws if checksum mismatch
- Throws if chunk too large

---

#### CompleteUpload(mediaFileId, shopId) → MediaFile

**Purpose**: Mark upload as complete and verify all chunks.

**Process**:
1. Find MediaFile by Id and ShopId
2. Verify all expected chunks received
3. Reconstruct file from chunks
4. Set Status to Active
5. Set CompletedAt timestamp
6. Extract metadata (video only)
7. Return updated MediaFile

**Constraints**:
- All chunks must be Verified
- Total size must match declared size
- Metadata extraction on video

**Errors**:
- Throws if file not found
- Throws if chunks incomplete
- Throws if size mismatch

---

#### GetMediaUrl(mediaFileId, shopId, expirationMinutes) → MediaUrl

**Purpose**: Generate secure URL for accessing media.

**Process**:
1. Find MediaFile by Id and ShopId
2. Generate access token (if auth required)
3. Create URL with token/signature
4. Set expiration timestamp
5. Return MediaUrl

**Constraints**:
- MediaFile must exist and be Active
- ExpirationMinutes: 1-43200 (1 min to 30 days)
- Token generation based on file ID + timestamp

**Errors**:
- Throws if file not found or not Active
- Throws if expiration invalid

---

### Domain Service: MetadataExtractionService

Extracts video metadata using FFprobe or similar.

**Operations**:

#### ExtractVideoMetadata(filePath) → VideoMetadata

**Purpose**: Extract video properties.

**Process**:
1. Run FFprobe on file
2. Parse duration, resolution, codecs
3. Create VideoMetadata object
4. Return metadata

**Supported Formats**: MP4, WebM, AVI, MOV, MKV

**Errors**:
- Throws if not a video
- Throws if extraction fails
- Throws if FFprobe unavailable

---

## Data Structures

### MediaFile Table

```sql
CREATE TABLE [dbo].[MediaFiles] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [ShopId] [uniqueidentifier] NOT NULL,
    [FileName] [nvarchar](255) NOT NULL,
    [FileType] [int] NOT NULL,  -- 1=Image, 2=Video, 3=Graphics
    [ContentType] [nvarchar](100) NOT NULL,
    [SizeBytes] [bigint] NOT NULL,
    [StoragePath] [nvarchar](500) NOT NULL,
    [Status] [int] NOT NULL,  -- 1=Pending, 2=Active, 3=Failed, 4=Deleted
    [CreatedAt] [datetime2] NOT NULL,
    [CompletedAt] [datetime2] NULL,
    [CreatedBy] [uniqueidentifier] NOT NULL,
    [VideoMetadata] [nvarchar](max) NULL,  -- JSON
    
    CONSTRAINT [UQ_MediaFiles_StoragePath] 
        UNIQUE ([StoragePath]),
    
    CONSTRAINT [IX_MediaFiles_Query] 
        INDEX ON ([ShopId], [Status], [CreatedAt] DESC)
);
```

### UploadChunk Table

```sql
CREATE TABLE [dbo].[UploadChunks] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [MediaFileId] [uniqueidentifier] NOT NULL,
    [ChunkNumber] [int] NOT NULL,
    [SizeBytes] [bigint] NOT NULL,
    [ChecksumMD5] [nvarchar](32) NOT NULL,
    [Status] [int] NOT NULL,  -- 1=Pending, 2=Uploaded, 3=Verified, 4=Failed
    [UploadedAt] [datetime2] NULL,
    [StoragePath] [nvarchar](500) NOT NULL,
    
    CONSTRAINT [FK_UploadChunks_MediaFiles] 
        FOREIGN KEY ([MediaFileId]) REFERENCES [dbo].[MediaFiles]([Id]),
    
    CONSTRAINT [UQ_UploadChunks_Unique] 
        UNIQUE ([MediaFileId], [ChunkNumber]),
    
    CONSTRAINT [IX_UploadChunks_Query] 
        INDEX ON ([MediaFileId], [ChunkNumber])
);
```

---

## Stories Mapping

### S15: Upload Media (Single File)
- Service: MediaUploadService.InitializeUpload() + UploadChunk() + CompleteUpload()
- API: POST /api/media/upload (single endpoint with optional chunking)
- Validation: File type, size, content type
- Result: MediaFile persisted with Active status

### S16: Chunked Upload
- Service: MediaUploadService.UploadChunk()
- API: PUT /api/media/{mediaFileId}/chunks/{chunkNumber}
- Resume: Can retry failed chunks
- Result: Chunks persisted, reconstructed on completion

### S17: Media Metadata
- Service: MetadataExtractionService.ExtractVideoMetadata()
- Auto-extraction: On video upload completion
- Stored in: VideoMetadata JSON column
- Result: Duration, resolution, codec, bitrate available

### S18: Media URLs
- Service: MediaUploadService.GetMediaUrl()
- API: GET /api/media/{mediaFileId}/url
- Security: Token-based or signature-based
- Result: Time-limited or permanent URL

---

## Design Principles

✅ **Multi-tenant Isolation**: ShopId on all queries  
✅ **Chunked Support**: Resume on failed chunks  
✅ **Status Tracking**: Pending → Active/Failed progression  
✅ **Metadata Extraction**: Automatic on video upload  
✅ **Secure URLs**: Token or signature-based access  
✅ **Large File Support**: Up to 500MB per file, 100MB per chunk  
✅ **Checksum Verification**: MD5 for chunk integrity  

---

## Decisions for ADR

1. **Storage strategy**: Local vs Azure Blob (choose implementation)
2. **Chunking threshold**: When to require chunking (e.g., > 10MB)
3. **Metadata extraction**: Sync vs async processing
4. **URL expiration**: Default/max lifetime
5. **File naming**: How to generate unique paths
6. **Video codec support**: Which formats to extract metadata from

---

## Next: Technical Design (Stage 2)

Once domain model is approved:
- Storage implementation (local filesystem)
- FFprobe integration for metadata
- API endpoint design
- Chunked upload flow
- URL generation and security
