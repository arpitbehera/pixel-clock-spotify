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

### Lightweight UI Ticks

Add a configurable `progressUpdateIntervalMs` with a default of `1000`.
The existing clock update interval remains `1000ms`.

Use one lightweight dispatcher timer scheduled at the smaller configured
lightweight interval. On each tick, run each update only when its own interval
is due:

- the clock update runs at `clockUpdateIntervalMs` and refreshes the `HH:mm`
  clock text from local time
- the progress update runs at `progressUpdateIntervalMs`, updates elapsed
  playback text from a local playback-progress baseline, and updates the
  progress ratio and rendered progress width

This coalesces default clock and progress work into one UI-thread wake per
second. Lightweight updates perform no Windows media-session calls.

Raise WPF property-change notifications only for bindable values that changed:

- clock updates raise `ClockText` only when displayed `HH:mm` changes
- progress updates raise only changed `ElapsedText`, `ProgressRatio`, and
  `ProgressPixelWidth` values
- media snapshots raise only changed metadata, playback, and artwork values

Do not perform bitmap work or raise broad property-change notifications during
lightweight ticks.

When `progressUpdateIntervalMs` is below one second, calculate visible position
and ratio on each due progress update, but format and notify `ElapsedText` only
when its displayed whole-second value changes. Notify `ProgressPixelWidth` only
when its rendered width changes by at least `0.5` device-independent pixel.

### Media Poll

Change the default `mediaUpdateIntervalMs` to `5000`.

The media poll:

- starts immediately when the window loads
- runs asynchronously without blocking lightweight UI ticks
- prevents a second poll from starting while the previous poll is still active
- coalesces any number of poll requests received during an active poll into one
  follow-up poll, allowing at most one active poll and one pending poll
- requests the current Windows media-session snapshot
- updates title, artist, album, playback state, duration, and the local
  playback-progress baseline
- runs immediately after the user selects `Refresh`

When the user selects `Refresh`, reload configuration, apply visual and
placement settings, restart the timer intervals from the reloaded values, and
request a serialized media poll. Configuration interval changes take effect
without restarting the applet.

### Local Playback Progress

Store a baseline consisting of the latest reported playback position, the
playback kind, and a monotonic elapsed-time timestamp captured when the
snapshot was applied. Use a `Stopwatch`-style source so wall-clock corrections
cannot jump visible playback progress. Keep local wall time only for the clock
display. Tests inject or supply monotonic elapsed-time values.

For visible UI ticks:

- while playback is `Playing`, calculate visible position as the reported
  position plus elapsed monotonic time since the baseline
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
and convert the pixelated result into a frozen `32x32` WPF-bindable bitmap.
Render the cached bitmap at the album-art frame size using WPF nearest-neighbor
scaling. Do not allocate an explicitly upscaled production bitmap.

Thumbnail retrieval is best-effort. If reading thumbnail bytes from the
selected session fails, still return and apply the session metadata and
playback progress with `ThumbnailBytes = null`, show the placeholder, and log
the read failure once per selected track identity when practical.

Use a hash of the thumbnail bytes as artwork identity. Render artwork only when
the hash changes. Cache the last hash and WPF-bindable pixelated bitmap so
normal five-second media polls and one-second visible ticks reuse the existing
image.

The Windows-only album-art bitmap adapter owns thumbnail decoding, artwork
hashing, and rendered bitmap caching. `MainViewModel` passes thumbnail bytes to
the adapter and owns a nullable bindable `ArtworkImage` property. The adapter
returns the cached or newly rendered bitmap when artwork is valid and `null`
when artwork is unavailable or invalid. `MainWindow.xaml` displays a generated
Spotify-like pixel placeholder whenever `ArtworkImage` is `null`. The
placeholder uses simple green circle and wave motifs from the app palette and
does not bundle an official Spotify logo asset.

When a thumbnail is missing or decoding fails:

- clear any previously displayed artwork and show the generated Spotify-like
  pixel placeholder
- treat a missing thumbnail as normal session state without logging
- log a decode failure once per distinct thumbnail hash
- continue updating metadata and playback progress

## Album-Art Playback Control

Expose a media-session operation that selects the current Spotify session at
action time and toggles play/pause using the local Windows media-session API.
Return whether the toggle succeeded.

Polling and toggling share the same Spotify-session selection policy. Prefer a
playing Spotify session, then a paused Spotify session, then a stopped or
unknown Spotify session. Preserve Windows media-session manager enumeration
order as the tie-breaker within the same playback kind.

Pointer input outside the album-art frame preserves the existing immediate
applet drag interaction. The album-art frame recognizes two gestures:

- a short press and release within a small movement threshold toggles
  play/pause
- movement beyond the threshold becomes the existing applet drag interaction
  and does not toggle playback

Use WPF's system drag-distance thresholds so the gesture matches normal Windows
pointer behavior.

After a successful toggle, request a media poll immediately so playback kind
and the local playback-progress baseline resynchronize without waiting for the
next five-second poll. Poll execution remains serialized: if a poll is already
active, run one follow-up poll after it completes rather than overlapping it.
Do not optimistically change playback state or visible progress after a
successful toggle. Keep the current baseline unchanged until the follow-up
snapshot arrives.

When no controllable Spotify session is available, clicking the album-art
placeholder does nothing and logs the failure once for that click attempt.
If the Windows toggle API returns `false`, return failure and log once for that
click attempt. If the toggle API throws, catch the exception at the application
edge, log the error, and leave the UI unchanged. Only a successful toggle
requests an immediate serialized media poll.

When `clickThrough` is `true`, preserve the existing whole-window click-through
behavior. Pointer input passes to windows behind the applet, so album-art
playback control and dragging are unavailable. Album-art playback control and
dragging are active only when `clickThrough` is `false`.

## Surrounding Window Layout

When `clickThrough` is `false`, keep other top-level application windows from
overlapping the docked applet while still allowing them to use display space
beside or below it. This is a partial-width exclusion area matching the docked
applet rectangle, not a full-width Windows AppBar strip.

Use an event-driven Windows-only `SurroundingWindowLayoutService`:

- subscribe with `SetWinEventHook` using `WINEVENT_OUTOFCONTEXT` and
  `WINEVENT_SKIPOWNPROCESS`
- observe move/resize completion and the top-level location changes needed for
  restored and newly shown windows
- coalesce burst notifications and inspect only the changed window
- enforce layout only after move/resize ends, after restore or show
  completion, or after the applet docks or changes relevant configuration
- suppress self-induced `SetWindowPos` notifications to prevent correction
  loops
- enumerate top-level windows only when the applet docks, changes display,
  changes size, or switches click-through mode
- call `SetWindowPos` only when a relevant top-level window overlaps the applet

For an overlapping window on the applet's display:

- preserve its size when possible
- consider positions beside and below the applet
- keep the result inside the same display work area
- choose the candidate requiring the smallest movement
- if neither candidate fits, resize minimally to fit the best candidate
- never move windows on another display

Apply this policy only to visible top-level application windows. Ignore the
applet itself, shell/taskbar/desktop windows, tool windows, owned dialogs and
popups, minimized windows, maximized windows, fullscreen windows, and
inaccessible windows. Native maximize behavior remains unchanged: a maximized
window may occupy the rectangular work area behind the topmost applet.

When `clickThrough` becomes `true`, remove the hooks and allow windows to move
under the applet. Do not restore windows that were previously moved; stop
enforcing future overlap only. Do not poll other windows. This keeps idle
resource use near zero and avoids retaining stale window bounds.

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

Validate interval lower bounds to prevent accidental CPU and Windows API churn:

- `clockUpdateIntervalMs` must be at least `250`
- `progressUpdateIntervalMs` must be at least `250`
- `mediaUpdateIntervalMs` must be at least `1000`

Values below these bounds fall back to their defaults.

## Architecture

Keep Windows-specific polling at the application edge and add local progress
calculation to the testable view-model layer.

- `AppConfig` and `ConfigLoader`: expose and validate the new progress interval
  and updated media default.
- `MainViewModel`: retain the latest media baseline, calculate visible progress
  from an injected or supplied monotonic elapsed-time timestamp, and expose
  nullable bindable `ArtworkImage` state.
- `WindowsMediaSessionService`: return selected-session thumbnail bytes and
  expose local play/pause toggling.
- `PixelArtRenderer`: keep pure pixel-array downsample/upscale logic.
- Windows-only album-art bitmap adapter: decode thumbnail bytes, hash artwork,
  reuse the cached frozen `32x32` rendered bitmap, convert the pure renderer
  output into a WPF-bindable bitmap, and return `null` for missing or invalid
  artwork.
- `MainWindow`: run a coalesced lightweight UI scheduler separately from a
  guarded media poll and classify album-art pointer input as click or drag.
- `MainWindow.xaml`: bind the artwork image, use packaged fonts, enlarge the
  clock, and add header lines.
- Windows-only `SurroundingWindowLayoutService`: when click-through mode is
  inactive, react to top-level window layout events and move only windows that
  overlap the docked applet.

## Error Handling

- If a media poll fails, log the error and keep the last successful snapshot.
- If thumbnail retrieval alone fails, apply the remaining media snapshot, show
  the placeholder, and log the read failure once per selected track identity
  when practical.
- Never start overlapping media polls.
- Stop local progress at the known duration.
- Preserve paused, stopped, and unknown positions until the next successful
  media snapshot.
- Fall back to `Consolas` if a packaged font cannot load.
- Preserve the placeholder when artwork is missing or cannot be decoded.
- Do not log missing artwork; log invalid artwork once per distinct thumbnail
  hash.
- Ignore unavailable-session album-art clicks and log once per click attempt.
- Do not poll surrounding windows. Ignore irrelevant or inaccessible top-level
  windows without destabilizing the applet.

## Testing

Automated tests cover:

- `progressUpdateIntervalMs` defaults to `1000`
- `mediaUpdateIntervalMs` defaults to `5000`
- invalid progress intervals fall back to the default
- clock and progress intervals below `250ms` fall back to their defaults
- media intervals below `1000ms` fall back to the default
- default clock and progress updates share one lightweight UI-thread wake per
  second
- media polling allows at most one active poll and one coalesced follow-up poll
- playing progress advances from its local baseline
- paused, stopped, and unknown progress do not advance
- visible progress clamps to duration
- a later media snapshot resynchronizes the local baseline
- selected-session thumbnail bytes propagate to the view model
- artwork is downsampled to a cached frozen `32x32` bitmap and WPF renders it
  at frame size with nearest-neighbor scaling
- unchanged artwork reuses the cached rendered bitmap
- missing or invalid artwork preserves the placeholder
- session selection prefers playing, then paused, then stopped or unknown
  Spotify sessions while preserving enumeration order within a playback kind
- play/pause control delegates to the selected Windows media session
- play/pause control returns quietly when no controllable session is available
- album-art input below the movement threshold classifies as a click
- album-art input beyond the movement threshold classifies as a drag
- surrounding-window notifications are coalesced and irrelevant windows are
  ignored
- overlapping windows are moved only while click-through mode is inactive
- surrounding-window layout preserves size when possible, chooses the
  smallest valid movement, ignores other displays, and ignores maximized
  windows

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
- when click-through mode is inactive, other application windows occupy space
  beside or below the applet without overlapping it
- when click-through mode is active, other windows may extend underneath the
  applet

## Out Of Scope

- Event-driven Windows media-session subscriptions
- Spotify Web API integration
- Layout changes beyond typography, clock sizing, and header treatment
