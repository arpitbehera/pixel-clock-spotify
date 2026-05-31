# Responsive Pixel UI Design

## Goal

Make the ambient applet update visibly once per second without polling Windows media sessions every second, and bring the UI closer to `docs/ref/TUI_reference.png`.

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
  progress from an injected or supplied timestamp.
- `MainWindow`: run lightweight UI timers separately from a guarded media poll.
- `MainWindow.xaml`: use packaged fonts, enlarge the clock, and add header
  lines.

## Error Handling

- If a media poll fails, log the error and keep the last successful snapshot.
- Never start overlapping media polls.
- Stop local progress at the known duration.
- Preserve paused, stopped, and unknown positions until the next successful
  media snapshot.
- Fall back to `Consolas` if a packaged font cannot load.

## Testing

Automated tests cover:

- `progressUpdateIntervalMs` defaults to `1000`
- `mediaUpdateIntervalMs` defaults to `5000`
- invalid progress intervals fall back to the default
- playing progress advances from its local baseline
- paused, stopped, and unknown progress do not advance
- visible progress clamps to duration
- a later media snapshot resynchronizes the local baseline

Manual Windows verification covers:

- the clock visibly updates independently of media polling
- elapsed time and progress move once per second while Spotify is playing
- media metadata resynchronizes approximately every five seconds
- polling does not overlap when Windows media APIs respond slowly
- block clock digits fill the available left-panel height without clipping
- metadata uses the readable pixel font
- the media header is centered between horizontal green lines

## Out Of Scope

- Event-driven Windows media-session subscriptions
- Spotify Web API integration
- Album-art decoding or thumbnail rendering changes
- Layout changes beyond typography, clock sizing, and header treatment
