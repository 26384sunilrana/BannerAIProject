# Bolt 034 - Media foundation (local disk)

Status: complete. Azure Blob is deliberately left for the end (bolt 044), as decided.

## Built
- **Size and length read from the file** when an upload completes: PNG, JPEG, GIF, WebP pictures and MP4, WebM videos, parsed from the headers in C# (no ffprobe).
  A file that cannot be understood gets no size, never an error. Stored as `Width`, `Height`, `DurationSeconds` on the media record.
- **Library API** (`/api/media`): list (newest first, filter by image/video, search by name, paging, a signed link per file, "in use" flag),
  `usage` (bytes, counts, and the plan's storage allowance when the shop has a plan), `DELETE` (owner and sales executives).
- **Delete protection:** a file used by a banner, or kept in any earlier version of one (a restore would bring it back), is refused with 409 and the banners are named.
  Otherwise the bytes and any leftover pieces are removed and the record stays as a "deleted" marker for 30 days.
- **Clean-up job** (`MediaCleanupWorker` + `MediaCleanupService`): removes uploads never finished within 24 h and failed uploads, drops old deleted records.
  Settings under `Media:Cleanup:*`; `POST /api/media/admin/cleanup` runs it now (admin).
- **Screens:** "My files" (grid with picture or first video frame, size, dimensions, length, used/not used, storage bar, search, filter, upload, delete with confirmation);
  "Choose from my files" in the editor for image and video components; menu entry and home card.
- Migration `MediaLibrary`.

## Fixed on the way
- Deleting a file while a browser was streaming it failed with a 500 on Windows (the file was locked). Files are now opened so they can be deleted while streamed,
  and a lock that still remains never fails the delete; the clean-up retries.
- The `Select` component could not change its empty choice text ("Select an option"); it now takes a `placeholder`.

## Verified
- Domain 585, Application 188, Integration 15 (also on SQL Server LocalDB with the real migration), Jest 573, tsc clean, next build OK.
- Chrome against the real API: 13/13 checks (empty library, picture size 2x2 and video 640x360 / 0:13 read from the files, storage counted, search, filter,
  picker offers only images, chosen file shows in the banner, used file marked and its delete refused naming the banner, unused file deleted, storage drops).

## Not done
See pending.md section D. Storage limit is shown but not enforced yet (bolt 039).
