---
id: 027-editor-api-alignment
unit: banner-editor-ui
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [editor works against the real API]
---

# Bolt: 027-editor-api-alignment

The editor was written against an imagined API: it could not load, add or save anything on the real one. It now does.

## UI
- bannerMapper/bannerService: one adapter between the editor model (type names, x/y/width/height, data) and the API (numeric types, PositionX/SizeWidth..., flat Properties). Loads banner + components (banner and /preview), adds, updates, deletes. Values are kept inside what the API accepts (no negative positions, size 1-5000, layer 0-100).
- Saving: sends full components, reports every failure instead of "saved" (it used to swallow errors), keeps the banner marked unsaved on failure.
- Editing text, colour, shape and (new) image/video fields now changes the component's content (before, they were written to the wrong place and nothing changed on the canvas).
- Layers: new components take the next free layer; changing a layer uses the server reorder (which swaps with the component on that layer) after saving pending edits. Layer service routes corrected.
- Dragging and resizing never worked (mouse movement was not wired up; handles could not be clicked because of inherited pointer-events: none; west/north resizes drifted). Fixed, with zoom taken into account.
- The editor reloaded the banner in an endless loop (toast object in effect dependencies). Fixed.
- Preview button works; video components render; shapes can be circles; back link to the banner list; the editor needs sign-in.
- Chunk uploads send the MD5 header the server checks.

## API
- Adding a component to a loaded banner failed with 500 on SQL Server: ids are assigned when a component is created, so EF treated the new component as existing and issued an UPDATE. BannerRepository.UpdateAsync now inserts components that are not in the database.
- Swapping two components' layers failed (409) because of the unique (banner, layer) index. Layer changes now go through SaveLayerChangesAsync, which parks moving components on temporary layers inside a transaction.

## Verification
- Chrome against the real API on SQL Server LocalDB: 18 editor checks (open, add, edit content, drag, resize, add shape, layer swap, image URL, save, reload, positions persisted, preview, delete, submit for approval, no API error) plus the earlier 28 account/team/approval checks, all passing.
- Domain 419, Application 109, Integration 7 tests pass; Jest 383 passing (114 older tests were already failing; 118 before this work).

## Not done
- Image and video files cannot be uploaded from the editor (only an address can be entered); the upload hook exists but has no screen, and its endpoints were not checked against the media API.
- Video playlists (bolt 020) have no editor controls.
- Undo/redo buttons are inert; no background colour (the API has none); effects and carousels have no editor controls.
- The older 114 failing Jest tests were not repaired.
