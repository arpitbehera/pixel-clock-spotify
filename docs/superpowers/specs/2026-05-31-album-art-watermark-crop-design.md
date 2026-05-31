# Album Art Watermark Crop Design

## Context

Rendered album art can include a Spotify watermark in the bottom strip. The app does not draw that watermark in XAML. Album art bytes come from Windows media sessions and are decoded by `AlbumArtBitmapAdapter`, then pixelated to a 32x32 bitmap by `PixelArtRenderer.Downsample`.

## Goal

Remove the visible watermark from rendered album art without adding Spotify API calls or changing the UI layout.

## Approach

Crop a small bottom strip from the decoded artwork before pixelation. This keeps the fix local to the existing artwork pipeline and avoids masking after pixelation, where the watermark has already affected averaged pixels.

Rejected alternatives:

- Add a black overlay in XAML: hides output but leaves watermark colors mixed into the pixelated image.
- Fetch album art from Spotify Web API: higher quality, but requires auth, networking, caching, failure handling, and more configuration.

## Design

Add a crop helper to `PixelArtRenderer` that returns a `BgraImage` with the bottom percentage removed. Use it in `AlbumArtBitmapAdapter.Decode` before calling `Downsample`.

Initial crop amount: 15% of source height. Clamp so at least one source row remains. This removes the watermark shown in `docs/ref/TUI_V3.png` while keeping behavior deterministic and easy to test.

## Testing

Add unit coverage for the crop helper proving bottom rows are excluded. Existing renderer tests continue to cover downsample and nearest-neighbor behavior.

## Scope

No UI layout changes, no new config, no external services, no changes to media session selection.
