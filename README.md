# Terminal Clock Spotify

Windows 10 WPF Ambient Desktop Applet showing a pixel-terminal clock and local Spotify now-playing state.

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
