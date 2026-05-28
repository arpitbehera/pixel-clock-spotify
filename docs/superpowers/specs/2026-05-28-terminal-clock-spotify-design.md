# Terminal Clock Spotify Applet Design

## Goal

Build a low-resource Windows 10 desktop applet that visually matches the reference image: a black, pixel-terminal style panel with a large `HH:mm` clock on the left and Spotify now-playing details on the right, including faithful pixelated album art.

## Target Platform

- Windows 10 LTSC 64-bit, version 10.0 build 19044.
- Primary implementation: C# WPF desktop app.
- Target framework: `.NET 8` WPF using `net8.0-windows10.0.19041.0`.
- Publish mode is framework-dependent to minimize installed app size.
- The app is per-monitor DPI aware.
- The app is an Ambient Desktop Applet: a borderless desktop window styled like a terminal, not a real terminal process.
- The app does not appear in the taskbar or Alt-Tab and does not take focus when it starts.
- The app is always on top by default.
- The app is not click-through by default; users are expected to place normal windows around it rather than underneath it.
- The app can be moved by left-click dragging anywhere on the applet, but only between Dock Positions.
- The app starts at login and appears at the top-left corner of the Target Display.

The Target Display is the monitor labeled `2` in Windows Display Settings when that label can be discovered. If the Windows display label cannot be discovered, the app uses a configured monitor match such as device name or bounds. If no configured match is valid, the app falls back to the top-left corner of the primary display.

The default applet bounds are anchored to the top-left Dock Position of the Target Display. A Dock Position is the top-left or top-right corner of any detected display. The default width is `50%` of the Target Display width, and the default height is `25%` of the Target Display height.

When the user drags the applet to another Dock Position, the app persists the last Dock Position in `%APPDATA%\TerminalClockSpotify\state.json`. Runtime state is stored separately from `appsettings.json` so config remains the source of default behavior. `Refresh` reloads config and keeps the current persisted Dock Position unless the configured display or size makes it invalid.

During a drag, the window may move freely for visual feedback. On drag release, the app computes every allowed Dock Position across detected displays and snaps to the nearest one using the applet's top-left point.

Placement and sizing account for mixed-DPI displays. Monitor bounds are converted into WPF device-independent units before applying window size and Dock Position coordinates.

## Visual Design

The first screen is the entire app. It uses the reference layout:

- Outer thin pixel-style border in muted green.
- Black background using the configured background color at `80%` window opacity.
- Sharp Pixel Geometry throughout: square edges, pixel-aligned borders/dividers/progress bars, no rounded corners, no shadows, no blurred chrome, and nearest-neighbor image scaling.
- Large pixel-like clock on the left using invariant 24-hour `HH:mm` format with leading zero. The app does not use system locale 12-hour formatting and does not show seconds.
- Bundled Pixel Font for clock, header, and metadata text, with `Consolas` as a fallback if the bundled font cannot load.
- Text uses nearest-neighbor or aliased rendering where WPF and the bundled Pixel Font make it practical, but readability takes priority if fully aliased text renders poorly. Shapes and album art remain strictly sharp.
- Vertical divider between clock and media panel.
- Right media panel with header text `NOW PLAYING ON SPOTIFY`.
- Album art frame on the left side of the media panel.
- Track title, artist, and album text to the right of the art. Album name is always shown.
- Progress bar below metadata with elapsed and total time labels.

The layout uses fixed proportions so the UI does not resize or jump when metadata changes. Text that is too long is clipped or ellipsized inside its assigned region.

The outer window uses the configured display-relative bounds. The inner layout adapts proportionally inside those bounds:

- left clock region uses about `68%` of the inner width
- right media region uses about `32%` of the inner width
- divider stays between the two regions
- album art is square and sized from available media-region height
- title, artist, album, and progress stay clipped inside the media region
- long title, artist, and album values use ellipsis instead of hiding any metadata row

The visible UI has no buttons. A right-click context menu provides:

- `Refresh`: reload config and refresh media state
- `Config`: open `%APPDATA%\TerminalClockSpotify\appsettings.json`
- `Always On Top`: toggle topmost behavior live and persist it to config
- `Exit`: close the applet

## Configuration

The app reads `%APPDATA%\TerminalClockSpotify\appsettings.json` at startup. Defaults are embedded so the app still runs if the file is missing. On first run, the app creates the user config file from defaults. The published output also includes a default config template.

Configuration includes:

- target display label, default `2`
- dock position, default `top-left`
- optional target monitor device name or bounds override
- placement retry interval, default `2000ms`
- placement retry limit, default `30000ms`
- window width ratio, default `0.50` of Target Display width
- window height ratio, default `0.25` of Target Display height
- optional fixed window width and height overrides
- always on top, default `true`
- click through, default `false`
- opacity, default `0.80`
- bundled Pixel Font asset path
- clock update interval, default `1000ms`
- media/progress update interval, default `1000ms`
- startup shortcut name
- Spotify source app ID contains list, default `["Spotify"]`
- palette values:
  - background
  - primary green
  - dim green
  - primary text
  - secondary text
  - progress fill
  - progress track
  - warning/idle text
  - optional album-art pixelation settings

Palette values are standard hex colors such as `#9ad178`.

The app writes lightweight rolling logs to `%LOCALAPPDATA%\TerminalClockSpotify\logs\app.log`. Logs record startup, config, placement, media-session errors, and state transitions. The app does not log every clock or media tick. Log retention is `3` files of `256KB` each.

## Spotify Integration

The app uses local Windows media session APIs instead of Spotify Web API.

Implementation starts with a small WinRT media-session spike that lists available media sessions and reads Spotify title, artist, album, thumbnail availability, playback status, and timeline position. The full UI work should proceed only after this spike proves the API references and runtime behavior on the target Windows environment.

Expected data source:

- `GlobalSystemMediaTransportControlsSessionManager`
- Spotify session selected by source application identifier when available
- session media properties for title, artist, album, thumbnail
- session timeline properties for position and duration
- playback status for playing, paused, or stopped

No OAuth flow, Spotify developer app, or network polling is required.

The media session selector matches sessions whose source application identifier contains any configured Spotify token case-insensitively. The default token list is `["Spotify"]` to support both desktop and Microsoft Store install identifiers. If multiple matching sessions exist, the selector prefers a playing session; otherwise it uses the most recently active matching session.

Progress uses the timeline position reported by Windows on each media tick. The app does not interpolate position locally between ticks; paused playback remains at the last reported paused position.

When Spotify is unavailable, stopped, or hidden by Windows media-session limitations, the app keeps the same layout and displays an idle state such as `NO SPOTIFY SESSION`, with a generated Spotify-like pixel placeholder in the album-art frame and no progress fill.

## Album Art Rendering

Album art is rendered as a faithful pixel-art approximation:

- Decode thumbnail from the Windows media session.
- Downsample to a small fixed grid.
- Preserve the cover's recognizable colors by default.
- Optionally apply mild quantization if configured.
- Scale back up using nearest-neighbor sampling.
- Cache the rendered bitmap per track/art identity.

The album-art renderer runs only when the track or thumbnail changes. Normal progress updates reuse the cached bitmap.

If a track has no thumbnail or thumbnail decoding fails, the album-art frame displays a generated Spotify-like pixel placeholder using the app palette. The placeholder uses simple green circle/wave motifs and does not bundle an official Spotify logo asset. Thumbnail failures are logged once per track instead of every media tick.

## Resource Strategy

The app is designed to stay mostly idle:

- Clock updates once per second.
- Media state and timeline update every 1-2 seconds.
- Album art processing happens only on track changes.
- No browser engine, Electron runtime, background service, or network polling.
- No animations beyond progress/time changes.

C# WPF has a baseline runtime cost, but it keeps the implementation reliable for Windows media sessions, monitor placement, font rendering, and startup behavior.

## Startup Installation

The project includes PowerShell scripts:

- install script:
  - check that the required .NET Desktop Runtime is installed
  - print clear installation instructions if the runtime is missing
  - publish the app
  - copy output to a stable per-user install directory
  - create a shortcut in the current user's Windows Startup folder
- uninstall script:
  - remove the Startup shortcut
  - optionally remove the published app directory

The app does not require a Windows service or scheduled task. Version 1 uses only the Startup folder shortcut as the startup mechanism.

At login, the app positions itself immediately using the best available display, then retries Target Display lookup every `2000ms` for up to `30000ms`. The retry stops early once the Target Display is found. Spotify session discovery continues on the normal media update interval.

## Architecture

The app is split into focused units:

- WPF shell: owns window, layout, bindings, and startup lifecycle.
- configuration loader: reads `appsettings.json`, applies defaults, validates colors and dimensions.
- monitor placement service: selects the Target Display and computes DPI-aware Dock Position placement with fallback.
- clock service: formats local time as invariant 24-hour `HH:mm`.
- media session service: selects the Spotify media session and exposes now-playing state.
- album-art renderer: converts thumbnails into cached pixel-art bitmaps.
- startup scripts: install and uninstall published app startup behavior.

Each unit exposes simple data objects so pure logic can be tested without running the WPF UI.

## Error Handling

- Missing config: use defaults.
- Invalid config value: use the default for that field and log a warning.
- Missing Target Display: use primary display.
- Target Display appears after startup: move to the Target Display during the bounded startup retry window.
- Spotify not running: show idle state.
- Media session unavailable or access denied: show idle state and retry on the next media tick.
- Thumbnail decode failure: show the generated Spotify-like pixel placeholder and continue displaying text/progress.

Errors should not crash the app during normal startup.

## Testing

Automated tests cover logic that does not require an active Windows desktop session:

- invariant 24-hour `HH:mm` clock formatting.
- config defaults and palette parsing.
- display selection fallback behavior.
- mixed-DPI placement conversion behavior.
- media-session selection from candidate sessions.
- progress formatting and clamping.
- album-art downsample/upscale behavior using fixture images.

Manual verification covers:

- launches on Windows 10 LTSC.
- positions at top-left of the Target Display.
- falls back to primary display when the Target Display is absent.
- reads live Spotify desktop app metadata from Windows media sessions.
- updates progress while playing.
- shows stable idle state when Spotify is closed.
- appears on login after install script runs.

## Out Of Scope

- Spotify Web API integration.
- OAuth login.
- Full media controls such as play/pause/skip buttons.
- Cross-platform support.
- A full installer MSI/MSIX.
- A real terminal/console rendering mode.
