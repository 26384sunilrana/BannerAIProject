---
id: 020-video-rotation
unit: banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [4.5, 4.6]
---

# Bolt: 020-video-rotation

## Delivered
- A video component can hold a playlist: properties { playlist:[{mediaUrl, durationSeconds?}], rotationMode: "playFull" | "fixedSeconds", secondsPerVideo, muted, volume, loop }.
- playFull runs each video to its end; fixedSeconds runs each for N seconds. A video shorter than N rotates when it ends (the shorter value is used, and every step sets advanceWhenEnded).
- Muted forces volume 0; otherwise the configured 0-1 volume applies.
- Validation (1-20 videos, valid urls, secondsPerVideo 1-3600 for fixedSeconds, volume 0-1) runs when a video component is added or updated.
- Component and preview responses carry a ready-made playbackPlan (steps, cycleSeconds) for the player.
- Single-video components keep working unchanged (legacy properties are not validated).
- Tests: VideoPlaylistTests (10).

## Not done
- Video durations come from the client or media metadata; nothing fills durationSeconds automatically from the media service.
- The player (BannerUI) does not play playlists yet; bolt 026.
- The UI video type (mediaUrl, duration, resolution) and the backend VideoComponentProperties (VideoReference, Duration) still use different field names.
