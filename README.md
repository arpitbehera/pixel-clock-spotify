# Terminal Clock Spotify

Windows 10 WPF Ambient Desktop Applet showing a pixel-terminal clock and local Spotify now-playing state.

The applet updates its clock and locally extrapolated playback progress once per second while polling Windows media sessions every five seconds by default. Spotify artwork is rendered as cached `32x32` pixel art. When click-through mode is disabled, a short album-art click toggles play/pause, an album-art drag moves the applet, and nearby top-level application windows are moved beside or below the docked applet instead of overlapping it.

## Preview

![Terminal Clock Spotify applet showing a pixel clock reading 15:06 and the now-playing track "Metric Rules" by Spinall](docs/screenshots/applet-preview.png)

## Build

```powershell
dotnet build TerminalClockSpotify.sln
```

## Test

```powershell
dotnet test TerminalClockSpotify.sln
```

## Install

```powershell
.\scripts\install.ps1
```

User config is created at `%APPDATA%\TerminalClockSpotify\appsettings.json`.
Runtime state is stored at `%APPDATA%\TerminalClockSpotify\state.json`.
Logs are written to `%LOCALAPPDATA%\TerminalClockSpotify\logs\app.log`.

## Manual Windows Verification

- Confirm the clock and elapsed playback progress move once per second while Spotify is playing.
- Confirm metadata and artwork resynchronize after a track change within roughly five seconds.
- Confirm artwork is pixelated, recognizable, and scaled with sharp nearest-neighbor edges.
- Confirm a short album-art click toggles Spotify and dragging from the same frame still snaps the applet.
- Confirm click-through mode passes input to windows behind the applet.
- Confirm other restored application windows move beside or below the applet only while click-through mode is disabled.
