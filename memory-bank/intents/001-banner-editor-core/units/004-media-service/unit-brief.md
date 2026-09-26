---
unit: 004-media-service
intent: 001-banner-editor-core
phase: inception
status: draft
created: 2026-09-26T00:00:00Z
---

# Unit Brief: Media Service

## Purpose

ASP.NET Core microservice managing media file uploads and storage - images, videos (4K/8K), and graphics with metadata tracking and URL generation.

## Scope

### In Scope
- Media file upload (images, videos, graphics)
- Chunked upload support for large files
- File validation (type, size)
- Media metadata extraction (resolution, duration, codec)
- 4K/8K video support
- Storage management (local → Azure Blob)
- Media URL generation and signed URLs
- File deletion

### Out of Scope
- Media rendering/processing (transcoding)
- Media delivery optimization (CDN)
- Component management (Unit 001)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-6 | Video handling (4K/8K, timing, mute) | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description |
|--------|-------------|
| MediaFile | Aggregate root - uploaded file metadata |
| VideoMetadata | Value object - video-specific properties |
| UploadSession | Temporary state for chunked uploads |

### Key Operations
| Operation | Description |
|-----------|-------------|
| UploadMedia | Single or chunked file upload |
| GetMediaUrl | Retrieve access URL (with auth) |
| GetMediaMetadata | Video/image properties |
| DeleteMedia | Remove file and metadata |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 4 |

### Stories (Summary)

- S1: Upload image files
- S2: Upload video files (4K/8K support)
- S3: Chunked upload with resume
- S4: Retrieve media URLs and metadata

---

## Dependencies

### Depends On
- None (independent)

### Depended By
- Unit 001: Banner Service (media references)
- Unit 005: Banner Editor UI (media upload UI)

---

## Technical Context

### Technology
- Language: C# (.NET 8+)
- Framework: ASP.NET Core Web API
- Storage: Local filesystem (dev) → Azure Blob Storage (prod)
- Database: MSSQL (metadata only)

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Banner Service | Internal API | REST |
| Banner Editor UI | API | REST/multipart-form |
| Azure Blob Storage | SDK | Azure SDK |

### Data Storage
| Data | Type | Volume | Retention |
|------|------|--------|-----------|
| Media files | Blob | 100+ MB | Per banner |
| Media metadata | SQL | 100+ records | Per banner |
| Chunked uploads temp | FS | Temporary | Upload session |

---

## Constraints

- File size limit: 500MB (4K/8K videos)
- Supported formats: MP4, WebM, MOV, MKV, AVI (video); PNG, JPG, GIF (images)
- 4K/8K without quality loss
- Chunked upload with resume capability
- Multi-tenant isolation (shop_id context)

---

## Success Criteria

### Functional
- [x] Upload single/chunked files
- [x] Validate file types
- [x] Extract video metadata
- [x] Generate secure URLs
- [x] Support 4K/8K video

### Non-Functional
- [x] Resume capability on chunk fail
- [x] Large file upload fast enough
- [x] Metadata extraction < 1s

---

## Bolt Plan

| Bolt | Stories | Focus |
|------|---------|-------|
| Bolt-004 | S1, S2 | File upload + validation |
| Bolt-004b | S3, S4 | Chunked uploads + metadata |

---

## Notes

- Use FFprobe for video metadata extraction
- Implement temporary chunk storage cleanup
- Design storage abstraction (easy Azure Blob swap)
- Consider video encoding/transcoding (future)
- Implement rate limiting on uploads
