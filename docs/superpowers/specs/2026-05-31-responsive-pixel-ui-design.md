# Responsive Pixel UI Design

## Goal

Make the ambient applet update visibly once per second without polling Windows
media sessions every second, render recognizable pixelated album art, support
album-art click play/pause control, and bring the UI closer to
`docs/ref/TUI_reference.png`.

## Current Behavior

The current WPF shell creates two `DispatcherTimer` instances:

- a clock timer using `clockUpdateIntervalMs`
- a media timer using `mediaUpdateIntervalMs`

Every media tick starts an asynchronous refresh that requests a new
`GlobalSystemMediaTransportControlsSessionManager`, reads sessions, and fetches
media properties. The timer has no overlap guard, so another refresh can begin
while an earlier refresh is still running.

The current UI uses `Consolas` throughout. The left clock uses a fixed `118`
font size, leaving more unused space than the reference. The right panel uses a
plain left-aligned header without the horizontal lines shown in the reference.

The Windows media-session probe can detect thumbnail availability, but the
production media adapter always returns `ThumbnailBytes = null`. The pure
pixel-art renderer is not connected to WPF, and the XAML album-art frame is a
static placeholder. The whole applet currently participates in window dragging,
so album-art play/pause control needs an explicit click-versus-drag rule.

## Timing Design

Separate lightweight visible updates from Windows media-session polling.

### Visible UI Tick

Add a configurable `progressUpdateIntervalMs` with a default of `1000`.
The existing clock update interval remains `1000ms`.

The lightweight UI tick:

- refreshes the `HH:mm` clock text from local time
- updates elapsed playback text from a local playback-progress baseline
- updates the progress ratio and rendered progress width
- performs no Windows media-session calls

### Media Poll

Change the default `mediaUpdateIntervalMs` to `5000`.

The media poll:

- starts immediately when the window loads
- runs asynchronously without blocking lightweight UI ticks
- prevents a second poll from starting while the previous poll is still active
- requests the current Windows media-session snapshot
- updates title, artist, album, playback state, duration, and the local
  playback-progress baseline
- runs immediately after the user selects `Refresh`

### Local Playback Progress

Store a baseline consisting of the latest reported playback position, the
playback kind, and the local timestamp at which the snapshot was applied.

For visible UI ticks:

- while playback is `Playing`, calculate visible position as the reported
  position plus elapsed local wall-clock time since the baseline
- while playback is `Paused`, `Stopped`, or `Unknown`, keep the reported
  position unchanged
- clamp visible position to the range from zero to duration
- reset the baseline whenever a new media snapshot arrives

This keeps visible progress responsive while bounding drift to the interval
between media polls.

## Visual Design

Match the overall typography and right-panel treatment of
`docs/ref/TUI_reference.png` more closely while preserving the existing WPF
layout structure.

### Fonts

Bundle two redistributable font assets:

- a block-style pixel font for the clock digits
- a compact readable pixel font for the media header, metadata, and progress
  labels

Use WPF packaged font resource URIs and keep `Consolas` as the fallback family.
The two roles remain separate so the clock can use a strong 8-bit display style
without reducing metadata readability.

### Clock Panel

Increase the clock size so the digits use most of the available left-panel
height while remaining centered and unclipped. Preserve the invariant `HH:mm`
format and the existing left/right region split.

### Media Panel

Replace the plain header with a centered `NOW PLAYING ON SPOTIFY` label flanked
by horizontal green lines. Keep the existing square album-art area, metadata
rows, progress bar, elapsed label, and duration label.

Keep pixel-aligned geometry, sharp corners, nearest-neighbor bitmap scaling,
and ellipsis trimming for long metadata.

## Album Art Design

Read thumbnail bytes from the selected Windows media session when artwork is
available. Decode them into a pixel buffer, downsample to a fixed `32x32` grid,
and convert the pixelated result into a WPF-bindable bitmap. Render the bitmap
at the album-art frame size using nearest-neighbor scaling.

Use a hash of the thumbnail bytes as artwork identity. Render artwork only when
the hash changes. Cache the last hash and WPF-bindable pixelated bitmap so
normal five-second media polls and one-second visible ticks reuse the existing
image.

When a thumbnail is missing or decoding fails:

- clear any previously displayed artwork and show the existing album-art
  placeholder
- log the failure quietly
- continue updating metadata and playback progress

## Album-Art Playback Control

Expose a media-session operation that selects the current Spotify session at
action time and toggles play/pause using the local Windows media-session API.
Return whether the toggle succeeded.

The album-art frame recognizes two gestures:

- a short press and release within a small movement threshold toggles
  play/pause
- movement beyond the threshold becomes the existing applet drag interaction

Use WPF's system drag-distance thresholds so the gesture matches normal Windows
pointer behavior.

After a successful toggle, request a media poll immediately so playback kind
and the local playback-progress baseline resynchronize without waiting for the
next five-second poll. Poll execution remains serialized: if a poll is already
active, run one follow-up poll after it completes rather than overlapping it.

When no controllable Spotify session is available, clicking the album-art
placeholder does nothing and logs the failure quietly.

## Configuration

Add:

- `progressUpdateIntervalMs`, default `1000`

Change:

- `mediaUpdateIntervalMs`, default `5000`

Keep:

- `clockUpdateIntervalMs`, default `1000`

Existing user configuration files remain valid because missing values receive
defaults. Existing explicit `mediaUpdateIntervalMs` values continue to be
honored.

## Architecture

Keep Windows-specific polling at the application edge and add local progress
calculation to the testable view-model layer.

- `AppConfig` and `ConfigLoader`: expose and validate the new progress interval
  and updated media default.
- `MainViewModel`: retain the latest media baseline and calculate visible
  progress from an injected or supplied timestamp; retain current rendered
  artwork or the placeholder state.
- `WindowsMediaSessionService`: return selected-session thumbnail bytes and
  expose local play/pause toggling.
- `PixelArtRenderer`: keep pure pixel-array downsample/upscale logic.
- Windows-only album-art bitmap adapter: decode thumbnail bytes, hash artwork,
  reuse the cached rendered bitmap, and convert the pure renderer output into a
  WPF-bindable bitmap.
- `MainWindow`: run lightweight UI timers separately from a guarded media poll
  and classify album-art pointer input as click or drag.
- `MainWindow.xaml`: bind the artwork image, use packaged fonts, enlarge the
  clock, and add header lines.

## Error Handling

- If a media poll fails, log the error and keep the last successful snapshot.
- Never start overlapping media polls.
- Stop local progress at the known duration.
- Preserve paused, stopped, and unknown positions until the next successful
  media snapshot.
- Fall back to `Consolas` if a packaged font cannot load.
- Preserve the placeholder when artwork is missing or cannot be decoded.
- Ignore unavailable-session album-art clicks and log them quietly.

## Testing

Automated tests cover:

- `progressUpdateIntervalMs` defaults to `1000`
- `mediaUpdateIntervalMs` defaults to `5000`
- invalid progress intervals fall back to the default
- playing progress advances from its local baseline
- paused, stopped, and unknown progress do not advance
- visible progress clamps to duration
- a later media snapshot resynchronizes the local baseline
- selected-session thumbnail bytes propagate to the view model
- artwork is downsampled to `32x32` and upscaled with nearest-neighbor sampling
- unchanged artwork reuses the cached rendered bitmap
- missing or invalid artwork preserves the placeholder
- play/pause control delegates to the selected Windows media session
- play/pause control returns quietly when no controllable session is available
- album-art input below the movement threshold classifies as a click
- album-art input beyond the movement threshold classifies as a drag

Manual Windows verification covers:

- the clock visibly updates independently of media polling
- elapsed time and progress move once per second while Spotify is playing
- media metadata resynchronizes approximately every five seconds
- polling does not overlap when Windows media APIs respond slowly
- block clock digits fill the available left-panel height without clipping
- metadata uses the readable pixel font
- the media header is centered between horizontal green lines
- real Spotify artwork appears as a recognizable pixelated `32x32` rendering
- a short album-art click toggles Spotify play/pause
- dragging from the album-art frame still moves and snaps the applet
- clicking the placeholder with no Spotify session has no visible effect

## Out Of Scope

- Event-driven Windows media-session subscriptions
- Spotify Web API integration
- Layout changes beyond typography, clock sizing, and header treatment
