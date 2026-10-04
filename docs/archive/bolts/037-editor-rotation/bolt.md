# Bolt 037 - Editor controls and screen playback

Status: complete

## Design decision
Effects, rotating pictures and rotating videos are stored **inside the component's own properties**, not in the older separate
Effects / Carousel API. The separate API never reached the preview, the banner versions or the shop screen; settings in the component
are saved, versioned, restored, previewed and approved with the banner for free.

## Built
- **Visual effect on any component** (requirement 4.3): fade in, slide in from the right/left/below, zoom in (play once), pulse and float (keep going),
  with how long it takes and when it starts.
- **Rotating pictures** on an image component (4.4, 4.7): a list of pictures (from "My files" or uploaded), order, seconds each (1-60),
  how it changes (fade, slide left/right, zoom) and how long the change takes. "Fit to banner" makes a component a full-size background;
  other components sit on top.
- **Rotating videos** on a video component (4.5, 4.6): a list, order, "the whole video then the next" or "N seconds each" (a video shorter than N moves on when it ends),
  muted or volume, repeat; shows the length of one round. Lengths come from the files (bolt 034).
- **One renderer for everything:** the editor (standing still, with small badges), Preview and the shop screen all draw components through
  `ComponentContent`, so what is designed is what plays. Text now follows its alignment in the editor too.
- **Undo / redo** with the header buttons and Ctrl+Z / Ctrl+Y / Ctrl+S: moves, resizes and setting changes, 50 steps, a drag or run of typing is one step.
- **Server checks:** effect, slides and playlist settings are validated on add and update (kinds, ranges, at most 20 pictures/videos); a playlist video is
  identified by its uploaded file, with a link optional. Links to files in a list are never stored (they expire); they are made when the banner opens.
- The media links of a rotation are fetched once per file.

## Verified
- Domain 622 (45 new), Application 222, Integration 37 (9 new), Jest 667, tsc clean, next build OK.
- Chrome against the real API with **real media** (three PNGs, two WebM clips recorded in the browser): 15/15 checks - the rotation is built in the editor,
  undo/redo, saved, survives a reload, rotates and plays its effect in Preview, and on the published banner on `/display` the pictures rotate,
  all load, the effect plays and the second video follows the first when it ends.
- The older editor (18), files-in-editor (11) and subscription-lifecycle / shop-screen (20) browser scripts still pass; the last two had to be brought up to date for the cookie session (they copied tokens out of local storage). The account, admin, places and library scripts were not re-run: this bolt does not touch them.
- An empty picture or video component now shows a dashed "No picture yet" / "No video yet" box in the editor so it can still be found.

## Not done
See backlog.md ("Editor and screen, left after bolt 037").
