# DDD-03: ADR Analysis — Media Service

**Status**: Stage 3 of 5 (ADR Analysis)  
**Bolt**: 006-media-service  
**Created**: 2026-09-26  

---

## ADR-001: Storage Provider (Local vs Cloud)

**Status**: ✅ ACCEPTED

### Problem
Where to store uploaded media files?
1. **Local Filesystem** — Project folder
2. **Azure Blob Storage** — Cloud storage
3. **Hybrid** — Local in dev, Azure in prod

### Decision
**Local for Bolt 006, Azure in production** (via IStorageProvider pattern).

### Rationale
- Local storage simpler for development/testing
- No Azure setup required locally
- Pattern allows swapping providers without code change
- User has local SQL Server (MAHASARASWATI)
- Migration to Azure Blob when deploying

### Implementation
- `IStorageProvider` interface (abstraction)
- `LocalStorageProvider` implementation
- DI container configurable per environment
- `appsettings.json`: StorageProvider setting

### Consequences
- ✅ Simple local testing
- ✅ No cloud dependencies
- ✅ Easy production migration
- ⚠️ File system limits (scaling)
- ⚠️ No built-in redundancy locally

### Alternatives Rejected
- Azure only: Requires local Azure setup complexity
- Custom file system: Overkill for Bolt 006

---

## ADR-002: Chunked Upload Threshold

**Status**: ✅ ACCEPTED

### Problem
When should uploads automatically chunk?
1. **Always chunk** — Even 1MB files
2. **Threshold-based** — Chunk if > 10MB
3. **Never chunk** — Single upload only

### Decision
**Threshold: Chunk if > 10MB**, else single upload.

### Rationale
- Balances simplicity with large file support
- 10MB threshold covers most images
- Videos almost always require chunking
- Reduces complexity for small files
- Resume capability valuable for 100MB+ videos

### Implementation
```csharp
ChunkingThreshold = 10485760  // 10MB
MaxChunkSize = 104857600       // 100MB per chunk
MaxFileSize = 536870912        // 500MB total
```

### Consequences
- ✅ Simple uploads for small files
- ✅ Chunking for video support
- ✅ Resume capability for large uploads
- ⚠️ Threshold configuration needed

---

## ADR-003: Metadata Extraction Timing

**Status**: ✅ ACCEPTED

### Problem
When to extract video metadata?
1. **Synchronous** — Extract immediately on completion
2. **Asynchronous** — Queue for background job
3. **Lazy** — Extract when first accessed

### Decision
**Synchronous** — Extract immediately on upload completion.

### Rationale
- Simpler implementation (no job queue)
- Metadata available immediately
- Metadata is required for rendering hints
- Video files smaller than typical local storage
- FFprobe execution time acceptable (<5s)

### Implementation
- Call `MetadataExtractionService` in `CompleteUploadAsync`
- Store result in VideoMetadata JSON column
- Catch extraction errors (non-blocking)

### Consequences
- ✅ Metadata available immediately
- ✅ No queue infrastructure needed
- ⚠️ Blocks upload completion on slow files
- 📋 Future: Could make async if needed

### Alternatives Rejected
- Async: Adds complexity (job queue, retry logic)
- Lazy: Unpredictable latency for first access

---

## ADR-004: URL Expiration Policy

**Status**: ✅ ACCEPTED

### Problem
How long should media URLs be valid?
1. **No expiration** — Permanent access
2. **Fixed lifetime** — Always 24 hours
3. **Configurable** — User specifies (1 min - 30 days)

### Decision
**Configurable**, default 30 days, max 30 days.

### Rationale
- Flexibility for different use cases
- 30-day default balances security and usability
- Cap prevents accidental permanent URLs
- Time-limited URLs expire old links
- Token-based validation

### Implementation
```csharp
public async Task<MediaUrlDto> GetMediaUrlAsync(
    Guid mediaFileId, Guid shopId, int? expirationMinutes = null)
{
    var minutes = expirationMinutes ?? 43200;  // 30 days default
    if (minutes > 43200) minutes = 43200;      // Cap at 30 days
    // Generate token valid until: DateTime.UtcNow.AddMinutes(minutes)
}
```

### Consequences
- ✅ Flexible for different needs
- ✅ Security via expiration
- ✅ No permanent URLs by default
- ⚠️ Expired URLs require regeneration

---

## ADR-005: File Naming Strategy

**Status**: ✅ ACCEPTED

### Problem
How to name files on disk?
1. **Original filename** — Keep user's filename
2. **UUID-based** — Unique identifier + extension
3. **Hash-based** — Content hash as name

### Decision
**UUID-based with subfolder per upload**.

### Rationale
- Collision-free naming
- Original filename preserved in metadata
- Folder per file for organization
- Easy to delete (folder removal)
- Supports multiple versions

### Implementation
```
/storage/uploads/{mediaFileId}/{mediaFileId}.bin
  or
/storage/uploads/{mediaFileId}/chunk_0
```

Storage path: `uploads/{mediaFileId}/{mediaFileId}.bin`

### Consequences
- ✅ Collision-free naming
- ✅ Easy organization
- ✅ Clean deletion
- ⚠️ Not human-readable on disk

---

## ADR-006: MD5 vs SHA256 Checksums

**Status**: ✅ ACCEPTED (MD5)

### Problem
What algorithm for chunk integrity verification?
1. **MD5** — Fast, weaker
2. **SHA256** — Slower, cryptographically secure
3. **Both** — Defensive

### Decision
**MD5 for chunk integrity** (not security critical).

### Rationale
- Chunk verification only (not auth)
- MD5 sufficient for corruption detection
- Fast computation even for 100MB chunks
- Industry standard for file integrity
- Upgradeable to SHA256 if needed

### Implementation
- Client computes MD5 of chunk
- Send as X-Checksum-MD5 header
- Server verifies: `MD5.ComputeHash(data) == claimed`
- Reject on mismatch

### Consequences
- ✅ Fast verification
- ✅ Detects corruption
- ⚠️ Not cryptographically secure
- 📋 Could upgrade to SHA256 later

---

## ADR-007: Chunk Status Tracking

**Status**: ✅ ACCEPTED

### Problem
How to track chunk upload progress?
1. **Simple** — Only Uploaded vs Failed
2. **Detailed** — Pending, Uploaded, Verified, Failed
3. **Very Detailed** — Add Queued, Processing, etc.

### Decision
**Four-state model**: Pending → Uploaded → Verified or Failed.

### Rationale
- Clear progression: Pending → Uploaded → Verified
- Can retry Failed chunks
- Verified confirms availability before final assembly
- Matches file-level status (Pending → Active)
- Simple but expressive

### Implementation
```csharp
public enum ChunkStatus
{
    Pending = 1,
    Uploaded = 2,
    Verified = 3,
    Failed = 4
}
```

### Consequences
- ✅ Clear state machine
- ✅ Resume capability
- ✅ Error tracking

---

## ADR-008: Video Format Support

**Status**: ✅ ACCEPTED

### Problem
Which video formats to support metadata extraction?
1. **All formats** — Accept any video
2. **Common formats** — MP4, WebM, MOV, MKV
3. **Strict list** — Only MP4 and WebM

### Decision
**Common formats: MP4, WebM, MOV, MKV** (metadata extraction best effort).

### Rationale
- MP4: Industry standard
- WebM: Web-native format
- MOV: Apple/professional
- MKV: Advanced container
- Covers 95% of use cases
- FFprobe supports all

### Implementation
```json
"AllowedMimeTypes": [
  "video/mp4",
  "video/webm",
  "video/quicktime",
  "video/x-matroska"
]
```

### Consequences
- ✅ Broad format support
- ✅ Metadata extraction works
- ⚠️ Rare formats rejected

---

## ADR-009: Single-Upload Simplicity

**Status**: ✅ ACCEPTED

### Problem
Should single-file uploads use same path as chunked?
1. **Same** — All through chunked upload logic
2. **Different** — Single uploads faster path
3. **Unified** — Chunked upload as single chunk

### Decision
**Unified: Single uploads as single chunk** (same path).

### Rationale
- One code path reduces bugs
- Single upload = 1-chunk upload
- No performance difference for small files
- Consistent error handling
- Simpler testing

### Implementation
- InitializeUpload + UploadChunk (single chunk) + Complete
- Same flow as multi-chunk, just totalChunks = 1

### Consequences
- ✅ Unified code path
- ✅ Less complex logic
- ⚠️ Slightly more overhead for small files
- ⚠️ Negligible impact

---

## Decision Summary Table

| ADR | Decision | Status | Risk |
|-----|----------|--------|------|
| ADR-001 | Local storage + provider pattern | ✅ ACCEPTED | Low |
| ADR-002 | Chunk if > 10MB threshold | ✅ ACCEPTED | Low |
| ADR-003 | Synchronous metadata extraction | ✅ ACCEPTED | Medium |
| ADR-004 | Configurable 30-day URL expiration | ✅ ACCEPTED | Low |
| ADR-005 | UUID-based file naming | ✅ ACCEPTED | Low |
| ADR-006 | MD5 for chunk integrity | ✅ ACCEPTED | Low |
| ADR-007 | Four-state chunk tracking | ✅ ACCEPTED | Low |
| ADR-008 | Common video formats support | ✅ ACCEPTED | Low |
| ADR-009 | Unified single/chunked upload flow | ✅ ACCEPTED | Low |

---

## Implementation Notes

1. **FFprobe Installation**: Required for video metadata
   - Local: `choco install ffmpeg` (Windows)
   - Docker: Include in container
   - Path: Configured in appsettings

2. **Storage Directory**: Must be created
   - Local: `./storage/uploads/` directory
   - Permissions: Application must have read/write

3. **Database Connection**: Uses same instance as Banner Service
   - Server: MAHASARASWATI
   - User: sa
   - Password: sa

---

## Next: Implementation (Stage 4)

Ready to implement based on these 9 architecture decisions.
