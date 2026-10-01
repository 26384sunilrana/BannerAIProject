---
id: 028-media-upload
unit: media-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [images and videos in banners, 4k/8k files]
---

# Bolt: 028-media-upload

The media service accepted uploads but never kept the bytes. Uploading now works from the editor to a playable link.

## API
- Chunk bytes are stored; completing assembles the pieces in order, checks there are no gaps and that the size matches what was announced, then removes the pieces. (Before, nothing was written to storage at all and the "download" address did not exist.)
- GET /api/media/{id}/download serves the file to a signed, expiring link (HMAC key from Media:SigningKey or derived from the JWT secret; the old link was an unkeyed hash anyone could compute). Range requests work, so videos can seek.
- Safety: only PNG, JPEG, GIF, WebP, MP4, WebM; the file's first bytes must match its type (a web page renamed .png is refused and deleted); files are served with nosniff and a sandbox policy so a stored file can never run as a page; storage paths cannot leave the storage folder.
- Fixed: upload initialisation always returned 401 (it read the "sub" claim, tokens carry the name identifier). Chunk size is now 8 MB and told to the client (it was 100 MB, above the web server's request limit).
- Resending a chunk replaces it (it used to leave duplicates that blocked completion).

## UI
- Image and video components get an upload control: type and size checks, MD5 per piece (spark-md5), progress bar, retries on server errors, server messages shown.
- The component stores the media file id; the expiring link is never saved and is made again whenever the banner opens.
- Pasting an address still works and replaces an uploaded file.

## Deployment
- Media volume (compose volume, Kubernetes ReadWriteMany claim banner-media, MediaService__LocalStoragePath=/media) and notes in DEPLOYMENT.md.

## Verification
- Chrome against the real API on SQL Server: 11 media checks (image shows and survives reload through a fresh link, only the id is saved, SVG and disguised file refused, 20 MB video sent as three pieces, ranged reads, stored bytes identical) plus the earlier 18 editor and 28 account checks.
- Domain 438, Application 109, Integration 9 tests pass; Jest 390 passing (103 older failures, down from 114).

## Not done
- Video length and resolution are not read from uploaded videos (the metadata service exists but is not called); 4K/8K is limited only by the 500 MB cap.
- Files are never deleted (no media library, no cleanup of unused uploads, no deleting a banner's files).
- Azure Blob storage provider; local disk only.
- No thumbnails or image resizing; very large images are sent as is.
- Uploads cannot be resumed after a page reload.
