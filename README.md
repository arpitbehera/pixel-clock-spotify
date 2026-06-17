# Terminal Clock Spotify

A lightweight Windows 10/11 **ambient desktop applet** showing a retro pixel-terminal clock and your local Spotify now-playing state. It sits quietly on the desktop — no taskbar entry, no Alt-Tab, no focus stealing — and stays out of the way of your other windows.

## Preview

![Terminal Clock Spotify applet showing a pixel clock reading 15:06 and the now-playing track "Metric Rules" by Spinall](docs/screenshots/applet-preview.png)

## Features

- **Pixel-terminal clock** updated once per second using a bundled retro bitmap font.
- **Spotify now-playing** — title, artist, album, and a locally extrapolated progress bar, read from Windows media sessions (no Spotify API key or login required).
- **Pixel-art album art** rendered as a sharp, cached `32x32` thumbnail with nearest-neighbor scaling.
- **Ambient by design** — no taskbar button, no Alt-Tab, never takes focus on launch.
- **Stays out of the way** — when click-through is off, nearby windows are nudged beside or below the applet instead of overlapping it.
- **Tiny, flat footprint** — see [Resource Usage](#resource-usage); designed to run all day without creeping CPU or memory.

## Resource Usage

A core goal of the applet is to stay invisible to your system, not just your eyes. Measured on Windows 11 with `dotnet-counters` and Task Manager:

| State | CPU | RAM (working set) | Managed heap |
| --- | --- | --- | --- |
| Idle (paused / no session) | ~0.4% | ~40 MB | 3–6 MB |
| Active (playing, 1s clock + 5s media poll) | <1% | ~40 MB | 3–6 MB |

Crucially, these numbers **stay flat over long runs** — a ~1 hour soak showed no upward drift in CPU, working set, or managed heap. (Earlier builds leaked up to ~15% CPU and 200 MB+ after a few hours; that has been fixed.)

### Verify it yourself

```powershell
dotnet tool install --global dotnet-counters
dotnet-counters monitor -n TerminalClockSpotify
```

Watch `GC Heap Size (MB)` and `GC Committed Bytes (MB)` stay flat. For working set, check `TerminalClockSpotify.exe` in Task Manager → Details.

## Install

```powershell
.\scripts\install.ps1
```

To remove:

```powershell
.\scripts\uninstall.ps1
```

User config is created at `%APPDATA%\TerminalClockSpotify\appsettings.json`.
Runtime state (display + dock position) is stored at `%APPDATA%\TerminalClockSpotify\state.json`.
Logs are written to `%LOCALAPPDATA%\TerminalClockSpotify\logs\app.log`.

## Controls

When **click-through mode is off**:

- **Short click on album art** — toggle Spotify play/pause.
- **Drag from album art** — move the applet; it snaps to the top-left or top-right corner of a display (a *dock position*).
- **Right-click** — menu for always-on-top, open config, reload, and exit.

When **click-through mode is on**, all pointer input passes through to the windows behind the applet (album-art control and dragging are unavailable).

## Configuration

Edit `%APPDATA%\TerminalClockSpotify\appsettings.json`, then reload from the right-click menu. Common settings:

| Key | Default | Meaning |
| --- | --- | --- |
| `targetDisplayLabel` | `"2"` | Monitor to show on, matching the label in Windows Display Settings. |
| `dockPosition` | `"top-left"` | Starting corner: `top-left` or `top-right`. |
| `clickThrough` | `false` | Pass all pointer input through to windows behind the applet. |
| `alwaysOnTop` | `true` | Keep the applet above other windows. |
| `opacity` | `0.8` | Window opacity, `0.0`–`1.0`. |
| `clockUpdateIntervalMs` | `1000` | Clock refresh interval. |
| `mediaUpdateIntervalMs` | `5000` | How often Windows media sessions are polled. |
| `spotifySourceAppIdContains` | `["Spotify"]` | Substrings used to pick the Spotify media session. |

## Build & Test

```powershell
dotnet build TerminalClockSpotify.sln
dotnet test TerminalClockSpotify.sln
```

## Manual Windows Verification

- Confirm the clock and elapsed playback progress move once per second while Spotify is playing.
- Confirm metadata and artwork resynchronize after a track change within roughly five seconds.
- Confirm artwork is pixelated, recognizable, and scaled with sharp nearest-neighbor edges.
- Confirm a short album-art click toggles Spotify and dragging from the same frame still snaps the applet.
- Confirm click-through mode passes input to windows behind the applet.
- Confirm other restored application windows move beside or below the applet only while click-through mode is disabled.
