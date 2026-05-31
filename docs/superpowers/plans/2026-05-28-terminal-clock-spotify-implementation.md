# Terminal Clock Spotify Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Windows 10 WPF Ambient Desktop Applet described in `docs/superpowers/specs/2026-05-28-terminal-clock-spotify-design.md`.

**Architecture:** Create a .NET 8 WPF app with pure services for configuration, clock formatting, placement, media-state selection, progress formatting, and album-art pixelation. Keep Windows-specific WinRT and WPF code at the edges so most behavior is covered by unit tests before UI work.

**Tech Stack:** C# 12, .NET 8, WPF, MSTest or xUnit, Windows Media Control WinRT APIs, PowerShell install scripts.

---

## File Structure

- Create `TerminalClockSpotify.sln`: solution containing app and tests.
- Create `src/TerminalClockSpotify/TerminalClockSpotify.csproj`: WPF app targeting `net8.0-windows10.0.19041.0`.
- Create `src/TerminalClockSpotify/App.xaml` and `App.xaml.cs`: WPF startup and dependency assembly.
- Create `src/TerminalClockSpotify/MainWindow.xaml` and `MainWindow.xaml.cs`: borderless applet window, layout, context menu, and drag snapping.
- Create `src/TerminalClockSpotify/appsettings.default.json`: embedded defaults copied to user config on first run.
- Create `src/TerminalClockSpotify/Config/AppConfig.cs`: strongly typed config records.
- Create `src/TerminalClockSpotify/Config/ConfigLoader.cs`: first-run config creation, validation, refresh, and persistence for topmost.
- Create `src/TerminalClockSpotify/Clock/ClockFormatter.cs`: invariant `HH:mm` clock formatting.
- Create `src/TerminalClockSpotify/Placement/DisplayInfo.cs`: monitor model in device-independent units.
- Create `src/TerminalClockSpotify/Placement/PlacementService.cs`: target display selection, sizing, and Dock Position snapping.
- Create `src/TerminalClockSpotify/Media/NowPlayingState.cs`: UI-ready media model.
- Create `src/TerminalClockSpotify/Media/MediaSessionSelector.cs`: pure Spotify session selection from candidates.
- Create `src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs`: WinRT adapter for `GlobalSystemMediaTransportControlsSessionManager`.
- Create `src/TerminalClockSpotify/Art/PixelArtRenderer.cs`: downsample/upscale cacheable album-art processing.
- Create `src/TerminalClockSpotify/Logging/RollingFileLogger.cs`: small rolling log writer.
- Create `src/TerminalClockSpotify/State/AppStateStore.cs`: persisted Dock Position in `%APPDATA%\TerminalClockSpotify\state.json`.
- Create `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`: bindable app state and timers.
- Create `scripts/install.ps1`: publish, copy to stable per-user app directory, create Startup shortcut.
- Create `scripts/uninstall.ps1`: remove Startup shortcut and optionally remove installed files.
- Create `tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj`: unit test project.
- Create test files beside the service boundaries: `ConfigLoaderTests.cs`, `ClockFormatterTests.cs`, `PlacementServiceTests.cs`, `MediaSessionSelectorTests.cs`, `ProgressFormatterTests.cs`, `PixelArtRendererTests.cs`.

## Implementation Tasks

### Task 1: Scaffold Solution

**Files:**
- Create: `TerminalClockSpotify.sln`
- Create: `src/TerminalClockSpotify/TerminalClockSpotify.csproj`
- Create: `tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj`
- Create: `src/TerminalClockSpotify/App.xaml`
- Create: `src/TerminalClockSpotify/App.xaml.cs`
- Create: `src/TerminalClockSpotify/MainWindow.xaml`
- Create: `src/TerminalClockSpotify/MainWindow.xaml.cs`

- [ ] **Step 1: Create the solution and projects**

Run:

```bash
rtk dotnet new sln -n TerminalClockSpotify
rtk dotnet new wpf -n TerminalClockSpotify -o src/TerminalClockSpotify --framework net8.0-windows
rtk dotnet new xunit -n TerminalClockSpotify.Tests -o tests/TerminalClockSpotify.Tests --framework net8.0
rtk dotnet sln TerminalClockSpotify.sln add src/TerminalClockSpotify/TerminalClockSpotify.csproj
rtk dotnet sln TerminalClockSpotify.sln add tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj
rtk dotnet add tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj reference src/TerminalClockSpotify/TerminalClockSpotify.csproj
```

Expected: solution and two projects are created.

- [ ] **Step 2: Edit the WPF project target and assets**

Set `src/TerminalClockSpotify/TerminalClockSpotify.csproj` to:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="appsettings.default.json" CopyToOutputDirectory="PreserveNewest" />
    <Resource Include="Assets\Fonts\*.ttf" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add a DPI-aware manifest**

Create `src/TerminalClockSpotify/app.manifest`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="TerminalClockSpotify.app"/>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    </windowsSettings>
  </application>
</assembly>
```

- [ ] **Step 4: Add a minimal WPF shell**

Set `src/TerminalClockSpotify/MainWindow.xaml` to a black bordered shell that can compile:

```xml
<Window x:Class="TerminalClockSpotify.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Terminal Clock Spotify"
        Width="1200"
        Height="260"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True">
  <Border BorderBrush="#9ad178" BorderThickness="2" Background="#CC000000">
    <Grid>
      <TextBlock Text="00:00"
                 Foreground="#9ad178"
                 FontFamily="Consolas"
                 FontSize="96"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center" />
    </Grid>
  </Border>
</Window>
```

- [ ] **Step 5: Run build**

Run:

```bash
rtk dotnet build TerminalClockSpotify.sln
```

Expected: build succeeds.

- [ ] **Step 6: Commit scaffold**

```bash
rtk git add TerminalClockSpotify.sln src tests
rtk git commit -m "chore: scaffold wpf app"
```

### Task 2: Configuration Defaults and Validation

**Files:**
- Create: `src/TerminalClockSpotify/appsettings.default.json`
- Create: `src/TerminalClockSpotify/Config/AppConfig.cs`
- Create: `src/TerminalClockSpotify/Config/ConfigLoader.cs`
- Test: `tests/TerminalClockSpotify.Tests/ConfigLoaderTests.cs`

- [ ] **Step 1: Write failing config tests**

Create `tests/TerminalClockSpotify.Tests/ConfigLoaderTests.cs`:

```csharp
using TerminalClockSpotify.Config;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ConfigLoaderTests
{
    [Fact]
    public void LoadCreatesDefaultConfigWhenMissing()
    {
        var root = TestDirectory.Create();
        var loader = new ConfigLoader(root.Path);

        var config = loader.Load();

        Assert.Equal("2", config.TargetDisplayLabel);
        Assert.Equal("top-left", config.DockPosition);
        Assert.True(config.AlwaysOnTop);
        Assert.Equal(0.50, config.WindowWidthRatio);
        Assert.Equal(0.25, config.WindowHeightRatio);
        Assert.True(File.Exists(Path.Combine(root.Path, "appsettings.json")));
    }

    [Fact]
    public void InvalidRatiosFallBackToDefaults()
    {
        var root = TestDirectory.Create();
        File.WriteAllText(Path.Combine(root.Path, "appsettings.json"), """
        { "windowWidthRatio": 2.0, "windowHeightRatio": -1.0, "opacity": 4.0 }
        """);
        var loader = new ConfigLoader(root.Path);

        var config = loader.Load();

        Assert.Equal(0.50, config.WindowWidthRatio);
        Assert.Equal(0.25, config.WindowHeightRatio);
        Assert.Equal(0.80, config.Opacity);
    }
}
```

Also create the test helper in the same file:

```csharp
internal sealed class TestDirectory : IDisposable
{
    public string Path { get; }

    private TestDirectory(string path) => Path = path;

    public static TestDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tcs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TestDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter ConfigLoaderTests
```

Expected: FAIL because `TerminalClockSpotify.Config` does not exist.

- [ ] **Step 3: Implement config model and loader**

Create `src/TerminalClockSpotify/Config/AppConfig.cs`:

```csharp
namespace TerminalClockSpotify.Config;

public sealed record AppConfig
{
    public string TargetDisplayLabel { get; init; } = "2";
    public string DockPosition { get; init; } = "top-left";
    public string[] SpotifySourceAppIdContains { get; init; } = ["Spotify"];
    public int PlacementRetryIntervalMs { get; init; } = 2000;
    public int PlacementRetryLimitMs { get; init; } = 30000;
    public double WindowWidthRatio { get; init; } = 0.50;
    public double WindowHeightRatio { get; init; } = 0.25;
    public double? FixedWindowWidth { get; init; }
    public double? FixedWindowHeight { get; init; }
    public bool AlwaysOnTop { get; init; } = true;
    public bool ClickThrough { get; init; }
    public double Opacity { get; init; } = 0.80;
    public int ClockUpdateIntervalMs { get; init; } = 1000;
    public int MediaUpdateIntervalMs { get; init; } = 1000;
    public string StartupShortcutName { get; init; } = "TerminalClockSpotify";
    public PaletteConfig Palette { get; init; } = new();
}

public sealed record PaletteConfig
{
    public string Background { get; init; } = "#000000";
    public string PrimaryGreen { get; init; } = "#9ad178";
    public string DimGreen { get; init; } = "#5f8f4d";
    public string PrimaryText { get; init; } = "#9ad178";
    public string SecondaryText { get; init; } = "#d7d7d7";
    public string ProgressFill { get; init; } = "#9ad178";
    public string ProgressTrack { get; init; } = "#4a4a4a";
    public string WarningIdleText { get; init; } = "#9ad178";
}
```

Create `src/TerminalClockSpotify/Config/ConfigLoader.cs` with `System.Text.Json`, camelCase options, default file creation, and validation that clamps invalid ratios, intervals, and opacity back to default values.

- [ ] **Step 4: Add default JSON**

Create `src/TerminalClockSpotify/appsettings.default.json`:

```json
{
  "targetDisplayLabel": "2",
  "dockPosition": "top-left",
  "spotifySourceAppIdContains": ["Spotify"],
  "placementRetryIntervalMs": 2000,
  "placementRetryLimitMs": 30000,
  "windowWidthRatio": 0.5,
  "windowHeightRatio": 0.25,
  "alwaysOnTop": true,
  "clickThrough": false,
  "opacity": 0.8,
  "clockUpdateIntervalMs": 1000,
  "mediaUpdateIntervalMs": 1000,
  "startupShortcutName": "TerminalClockSpotify",
  "palette": {
    "background": "#000000",
    "primaryGreen": "#9ad178",
    "dimGreen": "#5f8f4d",
    "primaryText": "#9ad178",
    "secondaryText": "#d7d7d7",
    "progressFill": "#9ad178",
    "progressTrack": "#4a4a4a",
    "warningIdleText": "#9ad178"
  }
}
```

- [ ] **Step 5: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter ConfigLoaderTests
```

Expected: PASS.

- [ ] **Step 6: Commit config**

```bash
rtk git add src/TerminalClockSpotify/Config src/TerminalClockSpotify/appsettings.default.json tests/TerminalClockSpotify.Tests/ConfigLoaderTests.cs
rtk git commit -m "feat: add app configuration"
```

### Task 3: Clock and Progress Formatting

**Files:**
- Create: `src/TerminalClockSpotify/Clock/ClockFormatter.cs`
- Create: `src/TerminalClockSpotify/Media/ProgressFormatter.cs`
- Test: `tests/TerminalClockSpotify.Tests/ClockFormatterTests.cs`
- Test: `tests/TerminalClockSpotify.Tests/ProgressFormatterTests.cs`

- [ ] **Step 1: Write failing formatting tests**

Create `tests/TerminalClockSpotify.Tests/ClockFormatterTests.cs`:

```csharp
using TerminalClockSpotify.Clock;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ClockFormatterTests
{
    [Theory]
    [InlineData(2026, 5, 28, 0, 5, "00:05")]
    [InlineData(2026, 5, 28, 21, 37, "21:37")]
    public void FormatUsesInvariantTwentyFourHourClock(int year, int month, int day, int hour, int minute, string expected)
    {
        var value = new DateTimeOffset(year, month, day, hour, minute, 44, TimeSpan.Zero);

        Assert.Equal(expected, ClockFormatter.Format(value));
    }
}
```

Create `tests/TerminalClockSpotify.Tests/ProgressFormatterTests.cs`:

```csharp
using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ProgressFormatterTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(203, "3:23")]
    [InlineData(413, "6:53")]
    [InlineData(3605, "60:05")]
    public void FormatDurationUsesMinuteSecondDisplay(int seconds, string expected)
    {
        Assert.Equal(expected, ProgressFormatter.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(-5, 100, 0.0)]
    [InlineData(50, 100, 0.5)]
    [InlineData(125, 100, 1.0)]
    [InlineData(30, 0, 0.0)]
    public void RatioClampsToProgressRange(double position, double duration, double expected)
    {
        Assert.Equal(expected, ProgressFormatter.Ratio(TimeSpan.FromSeconds(position), TimeSpan.FromSeconds(duration)), precision: 4);
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter "ClockFormatterTests|ProgressFormatterTests"
```

Expected: FAIL because formatter classes do not exist.

- [ ] **Step 3: Implement formatters**

Create `src/TerminalClockSpotify/Clock/ClockFormatter.cs`:

```csharp
using System.Globalization;

namespace TerminalClockSpotify.Clock;

public static class ClockFormatter
{
    public static string Format(DateTimeOffset value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);
}
```

Create `src/TerminalClockSpotify/Media/ProgressFormatter.cs`:

```csharp
namespace TerminalClockSpotify.Media;

public static class ProgressFormatter
{
    public static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            value = TimeSpan.Zero;

        var totalSeconds = (int)Math.Floor(value.TotalSeconds);
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    public static double Ratio(TimeSpan position, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return 0.0;

        return Math.Clamp(position.TotalSeconds / duration.TotalSeconds, 0.0, 1.0);
    }
}
```

- [ ] **Step 4: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter "ClockFormatterTests|ProgressFormatterTests"
```

Expected: PASS.

- [ ] **Step 5: Commit formatters**

```bash
rtk git add src/TerminalClockSpotify/Clock src/TerminalClockSpotify/Media/ProgressFormatter.cs tests/TerminalClockSpotify.Tests/*FormatterTests.cs
rtk git commit -m "feat: add clock and progress formatting"
```

### Task 4: Display Selection and Dock Placement

**Files:**
- Create: `src/TerminalClockSpotify/Placement/DisplayInfo.cs`
- Create: `src/TerminalClockSpotify/Placement/PlacementService.cs`
- Test: `tests/TerminalClockSpotify.Tests/PlacementServiceTests.cs`

- [ ] **Step 1: Write failing placement tests**

Create `tests/TerminalClockSpotify.Tests/PlacementServiceTests.cs`:

```csharp
using TerminalClockSpotify.Placement;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class PlacementServiceTests
{
    private static readonly DisplayInfo Primary = new("1", "\\\\.\\DISPLAY1", 0, 0, 1920, 1080, 1.0, true);
    private static readonly DisplayInfo Target = new("2", "\\\\.\\DISPLAY2", 1920, 0, 2560, 1440, 1.25, false);

    [Fact]
    public void SelectTargetUsesWindowsDisplayLabelWhenPresent()
    {
        var selected = PlacementService.SelectTarget([Primary, Target], "2", null);

        Assert.Equal(Target, selected);
    }

    [Fact]
    public void SelectTargetFallsBackToPrimaryWhenMissing()
    {
        var selected = PlacementService.SelectTarget([Primary], "2", null);

        Assert.Equal(Primary, selected);
    }

    [Fact]
    public void ComputeBoundsUsesDisplayRelativeRatioAndTopLeftDock()
    {
        var bounds = PlacementService.ComputeBounds(Target, "top-left", widthRatio: 0.5, heightRatio: 0.25, fixedWidth: null, fixedHeight: null);

        Assert.Equal(1920, bounds.Left);
        Assert.Equal(0, bounds.Top);
        Assert.Equal(1280, bounds.Width);
        Assert.Equal(360, bounds.Height);
    }

    [Fact]
    public void ComputeBoundsPlacesTopRightInsideTargetDisplay()
    {
        var bounds = PlacementService.ComputeBounds(Target, "top-right", widthRatio: 0.5, heightRatio: 0.25, fixedWidth: null, fixedHeight: null);

        Assert.Equal(3200, bounds.Left);
        Assert.Equal(0, bounds.Top);
        Assert.Equal(1280, bounds.Width);
        Assert.Equal(360, bounds.Height);
    }

    [Fact]
    public void SnapChoosesNearestAllowedDockPosition()
    {
        var snapped = PlacementService.SnapToNearest([Primary, Target], pointerLeft: 4300, pointerTop: 90, appWidth: 1000, appHeight: 300);

        Assert.Equal("\\\\.\\DISPLAY2", snapped.DisplayDeviceName);
        Assert.Equal("top-right", snapped.DockPosition);
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter PlacementServiceTests
```

Expected: FAIL because placement types do not exist.

- [ ] **Step 3: Implement placement service**

Create records with these signatures:

```csharp
namespace TerminalClockSpotify.Placement;

public sealed record DisplayInfo(
    string Label,
    string DeviceName,
    double Left,
    double Top,
    double Width,
    double Height,
    double DpiScale,
    bool IsPrimary);

public sealed record AppBounds(double Left, double Top, double Width, double Height);

public sealed record DockTarget(string DisplayDeviceName, string DockPosition, AppBounds Bounds);
```

Implement `PlacementService` with:

```csharp
namespace TerminalClockSpotify.Placement;

public static class PlacementService
{
    public static DisplayInfo SelectTarget(IReadOnlyList<DisplayInfo> displays, string targetLabel, string? deviceNameOverride)
    {
        if (!string.IsNullOrWhiteSpace(deviceNameOverride))
        {
            var byDevice = displays.FirstOrDefault(d => string.Equals(d.DeviceName, deviceNameOverride, StringComparison.OrdinalIgnoreCase));
            if (byDevice is not null)
                return byDevice;
        }

        var byLabel = displays.FirstOrDefault(d => string.Equals(d.Label, targetLabel, StringComparison.OrdinalIgnoreCase));
        if (byLabel is not null)
            return byLabel;

        return displays.FirstOrDefault(d => d.IsPrimary) ?? displays[0];
    }

    public static AppBounds ComputeBounds(DisplayInfo display, string dockPosition, double widthRatio, double heightRatio, double? fixedWidth, double? fixedHeight)
    {
        var width = fixedWidth ?? display.Width * widthRatio;
        var height = fixedHeight ?? display.Height * heightRatio;
        var left = string.Equals(dockPosition, "top-right", StringComparison.OrdinalIgnoreCase)
            ? display.Left + display.Width - width
            : display.Left;

        return new AppBounds(left, display.Top, width, height);
    }

    public static DockTarget SnapToNearest(IReadOnlyList<DisplayInfo> displays, double pointerLeft, double pointerTop, double appWidth, double appHeight)
    {
        var candidates = displays.SelectMany(display => new[]
        {
            new DockTarget(display.DeviceName, "top-left", new AppBounds(display.Left, display.Top, appWidth, appHeight)),
            new DockTarget(display.DeviceName, "top-right", new AppBounds(display.Left + display.Width - appWidth, display.Top, appWidth, appHeight)),
        });

        return candidates.OrderBy(c =>
        {
            var dx = c.Bounds.Left - pointerLeft;
            var dy = c.Bounds.Top - pointerTop;
            return dx * dx + dy * dy;
        }).First();
    }
}
```

- [ ] **Step 4: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter PlacementServiceTests
```

Expected: PASS.

- [ ] **Step 5: Commit placement logic**

```bash
rtk git add src/TerminalClockSpotify/Placement tests/TerminalClockSpotify.Tests/PlacementServiceTests.cs
rtk git commit -m "feat: add display placement logic"
```

### Task 5: Media State and Session Selection

**Files:**
- Create: `src/TerminalClockSpotify/Media/NowPlayingState.cs`
- Create: `src/TerminalClockSpotify/Media/MediaSessionSelector.cs`
- Test: `tests/TerminalClockSpotify.Tests/MediaSessionSelectorTests.cs`

- [ ] **Step 1: Write failing media selector tests**

Create `tests/TerminalClockSpotify.Tests/MediaSessionSelectorTests.cs`:

```csharp
using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class MediaSessionSelectorTests
{
    [Fact]
    public void SelectPrefersPlayingSpotifySession()
    {
        var sessions = new[]
        {
            new MediaSessionCandidate("Other.Player", "Song", MediaPlaybackKind.Playing, DateTimeOffset.UtcNow),
            new MediaSessionCandidate("Spotify.exe", "Track", MediaPlaybackKind.Playing, DateTimeOffset.UtcNow.AddSeconds(-10)),
            new MediaSessionCandidate("Spotify.Store", "Paused", MediaPlaybackKind.Paused, DateTimeOffset.UtcNow),
        };

        var selected = MediaSessionSelector.Select(sessions, ["Spotify"]);

        Assert.Equal("Spotify.exe", selected?.SourceApplicationId);
    }

    [Fact]
    public void SelectUsesMostRecentSpotifyWhenNonePlaying()
    {
        var old = DateTimeOffset.Parse("2026-05-28T10:00:00Z");
        var recent = DateTimeOffset.Parse("2026-05-28T10:10:00Z");

        var selected = MediaSessionSelector.Select([
            new MediaSessionCandidate("Spotify.exe", "Old", MediaPlaybackKind.Paused, old),
            new MediaSessionCandidate("Spotify.Store", "Recent", MediaPlaybackKind.Stopped, recent),
        ], ["Spotify"]);

        Assert.Equal("Spotify.Store", selected?.SourceApplicationId);
    }

    [Fact]
    public void SelectReturnsNullWhenSpotifyIsAbsent()
    {
        var selected = MediaSessionSelector.Select([
            new MediaSessionCandidate("Browser", "Video", MediaPlaybackKind.Playing, DateTimeOffset.UtcNow),
        ], ["Spotify"]);

        Assert.Null(selected);
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter MediaSessionSelectorTests
```

Expected: FAIL because media selector types do not exist.

- [ ] **Step 3: Implement media models and selector**

Create `src/TerminalClockSpotify/Media/NowPlayingState.cs`:

```csharp
namespace TerminalClockSpotify.Media;

public enum MediaPlaybackKind
{
    Unknown,
    Playing,
    Paused,
    Stopped
}

public sealed record MediaSessionCandidate(
    string SourceApplicationId,
    string Title,
    MediaPlaybackKind PlaybackKind,
    DateTimeOffset LastUpdated);

public sealed record NowPlayingState(
    string Header,
    string Title,
    string Artist,
    string Album,
    TimeSpan Position,
    TimeSpan Duration,
    MediaPlaybackKind PlaybackKind,
    byte[]? ThumbnailBytes)
{
    public static NowPlayingState Idle(string message) =>
        new("NOW PLAYING ON SPOTIFY", message, string.Empty, string.Empty, TimeSpan.Zero, TimeSpan.Zero, MediaPlaybackKind.Stopped, null);
}
```

Create `src/TerminalClockSpotify/Media/MediaSessionSelector.cs`:

```csharp
namespace TerminalClockSpotify.Media;

public static class MediaSessionSelector
{
    public static MediaSessionCandidate? Select(IReadOnlyList<MediaSessionCandidate> sessions, IReadOnlyList<string> spotifyTokens)
    {
        var matches = sessions
            .Where(session => spotifyTokens.Any(token => session.SourceApplicationId.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return matches
            .OrderByDescending(session => session.PlaybackKind == MediaPlaybackKind.Playing)
            .ThenByDescending(session => session.LastUpdated)
            .FirstOrDefault();
    }
}
```

- [ ] **Step 4: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter MediaSessionSelectorTests
```

Expected: PASS.

- [ ] **Step 5: Commit media selector**

```bash
rtk git add src/TerminalClockSpotify/Media tests/TerminalClockSpotify.Tests/MediaSessionSelectorTests.cs
rtk git commit -m "feat: add media session selection"
```

### Task 6: Album Art Pixel Renderer

**Files:**
- Create: `src/TerminalClockSpotify/Art/PixelArtRenderer.cs`
- Test: `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`

- [ ] **Step 1: Write failing pixel renderer tests**

Create `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`:

```csharp
using TerminalClockSpotify.Art;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class PixelArtRendererTests
{
    [Fact]
    public void DownsampleAveragesSourceCells()
    {
        var image = new BgraImage(2, 2, [
            new Bgra32(0, 0, 255, 255), new Bgra32(0, 255, 0, 255),
            new Bgra32(255, 0, 0, 255), new Bgra32(255, 255, 255, 255),
        ]);

        var result = PixelArtRenderer.Downsample(image, 1, 1);

        Assert.Equal(new Bgra32(127, 127, 127, 255), result.Pixels[0]);
    }

    [Fact]
    public void UpscaleUsesNearestNeighbor()
    {
        var image = new BgraImage(1, 1, [new Bgra32(10, 20, 30, 255)]);

        var result = PixelArtRenderer.UpscaleNearest(image, 2, 2);

        Assert.All(result.Pixels, pixel => Assert.Equal(new Bgra32(10, 20, 30, 255), pixel));
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter PixelArtRendererTests
```

Expected: FAIL because art renderer types do not exist.

- [ ] **Step 3: Implement pure pixel operations**

Create `src/TerminalClockSpotify/Art/PixelArtRenderer.cs` with:

```csharp
namespace TerminalClockSpotify.Art;

public readonly record struct Bgra32(byte B, byte G, byte R, byte A);

public sealed record BgraImage(int Width, int Height, IReadOnlyList<Bgra32> Pixels)
{
    public Bgra32 At(int x, int y) => Pixels[y * Width + x];
}

public static class PixelArtRenderer
{
    public static BgraImage Downsample(BgraImage source, int targetWidth, int targetHeight)
    {
        var pixels = new List<Bgra32>(targetWidth * targetHeight);

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var startX = x * source.Width / targetWidth;
                var endX = Math.Max(startX + 1, (x + 1) * source.Width / targetWidth);
                var startY = y * source.Height / targetHeight;
                var endY = Math.Max(startY + 1, (y + 1) * source.Height / targetHeight);
                var count = 0;
                var b = 0;
                var g = 0;
                var r = 0;
                var a = 0;

                for (var yy = startY; yy < endY; yy++)
                {
                    for (var xx = startX; xx < endX; xx++)
                    {
                        var pixel = source.At(xx, yy);
                        b += pixel.B;
                        g += pixel.G;
                        r += pixel.R;
                        a += pixel.A;
                        count++;
                    }
                }

                pixels.Add(new Bgra32((byte)(b / count), (byte)(g / count), (byte)(r / count), (byte)(a / count)));
            }
        }

        return new BgraImage(targetWidth, targetHeight, pixels);
    }

    public static BgraImage UpscaleNearest(BgraImage source, int targetWidth, int targetHeight)
    {
        var pixels = new List<Bgra32>(targetWidth * targetHeight);

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                pixels.Add(source.At(x * source.Width / targetWidth, y * source.Height / targetHeight));
            }
        }

        return new BgraImage(targetWidth, targetHeight, pixels);
    }
}
```

- [ ] **Step 4: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter PixelArtRendererTests
```

Expected: PASS.

- [ ] **Step 5: Commit pixel renderer**

```bash
rtk git add src/TerminalClockSpotify/Art tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs
rtk git commit -m "feat: add pixel album art renderer"
```

### Task 7: State Store and Logging

**Files:**
- Create: `src/TerminalClockSpotify/State/AppStateStore.cs`
- Create: `src/TerminalClockSpotify/Logging/RollingFileLogger.cs`
- Test: `tests/TerminalClockSpotify.Tests/AppStateStoreTests.cs`
- Test: `tests/TerminalClockSpotify.Tests/RollingFileLoggerTests.cs`

- [ ] **Step 1: Write failing persistence tests**

Create `tests/TerminalClockSpotify.Tests/AppStateStoreTests.cs`:

```csharp
using TerminalClockSpotify.State;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class AppStateStoreTests
{
    [Fact]
    public void SaveAndLoadRoundTripsDockPosition()
    {
        using var root = TestDirectory.Create();
        var store = new AppStateStore(root.Path);

        store.Save(new PersistedAppState("\\\\.\\DISPLAY2", "top-right"));

        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal("\\\\.\\DISPLAY2", loaded.DisplayDeviceName);
        Assert.Equal("top-right", loaded.DockPosition);
    }

    [Fact]
    public void LoadReturnsNullForMissingState()
    {
        using var root = TestDirectory.Create();
        var store = new AppStateStore(root.Path);

        Assert.Null(store.Load());
    }
}
```

Create `tests/TerminalClockSpotify.Tests/RollingFileLoggerTests.cs`:

```csharp
using TerminalClockSpotify.Logging;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class RollingFileLoggerTests
{
    [Fact]
    public void LoggerRetainsConfiguredNumberOfFiles()
    {
        using var root = TestDirectory.Create();
        var logger = new RollingFileLogger(root.Path, maxBytes: 4096, retainedFiles: 3);

        for (var i = 0; i < 20; i++)
            logger.Info(new string('x', 2048));

        var files = Directory.GetFiles(root.Path, "app*.log");
        Assert.InRange(files.Length, 1, 3);
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter "AppStateStoreTests|RollingFileLoggerTests"
```

Expected: FAIL because state and logging classes do not exist.

- [ ] **Step 3: Implement state store and rolling logger**

Create `src/TerminalClockSpotify/State/AppStateStore.cs`:

```csharp
using System.Text.Json;

namespace TerminalClockSpotify.State;

public sealed record PersistedAppState(string DisplayDeviceName, string DockPosition);

public sealed class AppStateStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public AppStateStore(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);
        _path = Path.Combine(rootDirectory, "state.json");
    }

    public PersistedAppState? Load()
    {
        if (!File.Exists(_path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PersistedAppState>(File.ReadAllText(_path), Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(PersistedAppState state)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(state, Options));
    }
}
```

Create `src/TerminalClockSpotify/Logging/RollingFileLogger.cs`:

```csharp
namespace TerminalClockSpotify.Logging;

public sealed class RollingFileLogger
{
    private readonly string _directory;
    private readonly int _maxBytes;
    private readonly int _retainedFiles;

    public RollingFileLogger(string logDirectory, int maxBytes = 262144, int retainedFiles = 3)
    {
        _directory = logDirectory;
        _maxBytes = maxBytes;
        _retainedFiles = retainedFiles;
        Directory.CreateDirectory(_directory);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception exception) => Write("ERROR", $"{message}: {exception}");

    private void Write(string level, string message)
    {
        RollIfNeeded();
        File.AppendAllText(CurrentPath, $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
    }

    private string CurrentPath => Path.Combine(_directory, "app.log");

    private void RollIfNeeded()
    {
        if (!File.Exists(CurrentPath) || new FileInfo(CurrentPath).Length < _maxBytes)
            return;

        for (var i = _retainedFiles - 1; i >= 1; i--)
        {
            var source = Path.Combine(_directory, i == 1 ? "app.log" : $"app.{i - 1}.log");
            var target = Path.Combine(_directory, $"app.{i}.log");
            if (File.Exists(target))
                File.Delete(target);
            if (File.Exists(source))
                File.Move(source, target);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter "AppStateStoreTests|RollingFileLoggerTests"
```

Expected: PASS.

- [ ] **Step 5: Commit persistence**

```bash
rtk git add src/TerminalClockSpotify/State src/TerminalClockSpotify/Logging tests/TerminalClockSpotify.Tests/*State* tests/TerminalClockSpotify.Tests/*Logger*
rtk git commit -m "feat: add state and logging"
```

### Task 8: Windows Media Session Adapter and Spike

**Files:**
- Create: `src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs`
- Create: `src/TerminalClockSpotify/Media/IMediaSessionService.cs`
- Create: `tools/MediaSessionProbe/MediaSessionProbe.csproj`
- Create: `tools/MediaSessionProbe/Program.cs`

- [ ] **Step 1: Add WinRT package references**

Edit `src/TerminalClockSpotify/TerminalClockSpotify.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Windows.SDK.Contracts" Version="10.0.22621.755" />
</ItemGroup>
```

- [ ] **Step 2: Create media service interface**

Create `src/TerminalClockSpotify/Media/IMediaSessionService.cs`:

```csharp
namespace TerminalClockSpotify.Media;

public interface IMediaSessionService
{
    Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Implement WinRT adapter**

Create `src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs` with this shape:

```csharp
using Windows.Media.Control;

namespace TerminalClockSpotify.Media;

public sealed class WindowsMediaSessionService : IMediaSessionService
{
    public async Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken)
    {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var sessions = manager.GetSessions()
            .Select(session => new MediaSessionCandidate(
                session.SourceAppUserModelId,
                string.Empty,
                MapPlayback(session.GetPlaybackInfo().PlaybackStatus),
                DateTimeOffset.UtcNow))
            .ToArray();

        var selected = MediaSessionSelector.Select(sessions, spotifyTokens);
        if (selected is null)
            return NowPlayingState.Idle("NO SPOTIFY SESSION");

        var session = manager.GetSessions().First(s => s.SourceAppUserModelId == selected.SourceApplicationId);
        var media = await session.TryGetMediaPropertiesAsync();
        var timeline = session.GetTimelineProperties();

        return new NowPlayingState(
            "NOW PLAYING ON SPOTIFY",
            string.IsNullOrWhiteSpace(media.Title) ? "UNKNOWN TRACK" : media.Title,
            media.Artist ?? string.Empty,
            media.AlbumTitle ?? string.Empty,
            timeline.Position,
            timeline.EndTime,
            selected.PlaybackKind,
            null);
    }

    private static MediaPlaybackKind MapPlayback(GlobalSystemMediaTransportControlsSessionPlaybackStatus status) =>
        status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackKind.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackKind.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackKind.Stopped,
            _ => MediaPlaybackKind.Unknown
        };
}
```

- [ ] **Step 4: Create console probe**

Run:

```bash
rtk dotnet new console -n MediaSessionProbe -o tools/MediaSessionProbe --framework net8.0-windows10.0.19041.0
rtk dotnet sln TerminalClockSpotify.sln add tools/MediaSessionProbe/MediaSessionProbe.csproj
rtk dotnet add tools/MediaSessionProbe/MediaSessionProbe.csproj package Microsoft.Windows.SDK.Contracts --version 10.0.22621.755
```

Set `tools/MediaSessionProbe/Program.cs` to list each media session source id, playback status, title, artist, album, position, duration, and thumbnail availability.

- [ ] **Step 5: Build and manually run probe on target Windows machine**

Run:

```bash
rtk dotnet build tools/MediaSessionProbe/MediaSessionProbe.csproj
rtk dotnet run --project tools/MediaSessionProbe/MediaSessionProbe.csproj
```

Expected on Windows with Spotify open: output includes one source id containing `Spotify`, a title, artist, album when available, timeline values, and thumbnail availability.

- [ ] **Step 6: Commit media adapter and probe**

```bash
rtk git add src/TerminalClockSpotify/Media tools/MediaSessionProbe src/TerminalClockSpotify/TerminalClockSpotify.csproj TerminalClockSpotify.sln
rtk git commit -m "feat: add windows media session adapter"
```

### Task 9: Main ViewModel and Timers

**Files:**
- Create: `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`
- Test: `tests/TerminalClockSpotify.Tests/MainViewModelTests.cs`

- [ ] **Step 1: Write failing ViewModel test**

Create `tests/TerminalClockSpotify.Tests/MainViewModelTests.cs`:

```csharp
using TerminalClockSpotify.Media;
using TerminalClockSpotify.ViewModels;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshMediaUpdatesBindableMetadata()
    {
        var service = new FakeMediaSessionService(new NowPlayingState(
            "NOW PLAYING ON SPOTIFY",
            "Time",
            "Pink Floyd",
            "The Dark Side of the Moon",
            TimeSpan.FromSeconds(203),
            TimeSpan.FromSeconds(413),
            MediaPlaybackKind.Playing,
            null));
        var viewModel = new MainViewModel(service, ["Spotify"]);

        await viewModel.RefreshMediaAsync(CancellationToken.None);

        Assert.Equal("Time", viewModel.Title);
        Assert.Equal("Pink Floyd", viewModel.Artist);
        Assert.Equal("The Dark Side of the Moon", viewModel.Album);
        Assert.Equal("3:23", viewModel.ElapsedText);
        Assert.Equal("6:53", viewModel.DurationText);
        Assert.Equal(203d / 413d, viewModel.ProgressRatio, precision: 4);
    }

    private sealed class FakeMediaSessionService(NowPlayingState state) : IMediaSessionService
    {
        public Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken) =>
            Task.FromResult(state);
    }
}
```

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter MainViewModelTests
```

Expected: FAIL because `MainViewModel` does not exist.

- [ ] **Step 3: Implement ViewModel**

Create `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TerminalClockSpotify.Clock;
using TerminalClockSpotify.Media;

namespace TerminalClockSpotify.ViewModels;

public sealed class MainViewModel(IMediaSessionService mediaSessionService, IReadOnlyList<string> spotifyTokens) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClockText { get; private set; } = ClockFormatter.Format(DateTimeOffset.Now);
    public string Header { get; private set; } = "NOW PLAYING ON SPOTIFY";
    public string Title { get; private set; } = "NO SPOTIFY SESSION";
    public string Artist { get; private set; } = string.Empty;
    public string Album { get; private set; } = string.Empty;
    public string ElapsedText { get; private set; } = "0:00";
    public string DurationText { get; private set; } = "0:00";
    public double ProgressRatio { get; private set; }
    public bool IsIdle { get; private set; } = true;

    public void RefreshClock(DateTimeOffset now)
    {
        ClockText = ClockFormatter.Format(now);
        OnPropertyChanged(nameof(ClockText));
    }

    public async Task RefreshMediaAsync(CancellationToken cancellationToken)
    {
        var state = await mediaSessionService.GetNowPlayingAsync(spotifyTokens, cancellationToken);
        Header = state.Header;
        Title = state.Title;
        Artist = state.Artist;
        Album = state.Album;
        ElapsedText = ProgressFormatter.Format(state.Position);
        DurationText = ProgressFormatter.Format(state.Duration);
        ProgressRatio = ProgressFormatter.Ratio(state.Position, state.Duration);
        IsIdle = state.PlaybackKind is MediaPlaybackKind.Stopped or MediaPlaybackKind.Unknown;

        OnPropertyChanged(string.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

- [ ] **Step 4: Run tests to verify green**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter MainViewModelTests
```

Expected: PASS.

- [ ] **Step 5: Commit ViewModel**

```bash
rtk git add src/TerminalClockSpotify/ViewModels tests/TerminalClockSpotify.Tests/MainViewModelTests.cs
rtk git commit -m "feat: add applet view model"
```

### Task 10: WPF Pixel-Terminal Layout

**Files:**
- Modify: `src/TerminalClockSpotify/MainWindow.xaml`
- Modify: `src/TerminalClockSpotify/MainWindow.xaml.cs`
- Modify: `src/TerminalClockSpotify/App.xaml.cs`

- [ ] **Step 1: Replace shell layout with reference layout**

Implement a borderless transparent WPF window with a green outer border, 68/32 column split, vertical divider, large left clock, right header, square album frame, metadata text rows, progress bar, elapsed and duration labels. Use `UseLayoutRounding="True"`, `SnapsToDevicePixels="True"`, `RenderOptions.BitmapScalingMode="NearestNeighbor"`, and ellipsis trimming for title, artist, and album.

- [ ] **Step 2: Bind layout to ViewModel**

Set bindings:

```xml
Text="{Binding ClockText}"
Text="{Binding Header}"
Text="{Binding Title}"
Text="{Binding Artist}"
Text="{Binding Album}"
Text="{Binding ElapsedText}"
Text="{Binding DurationText}"
Width="{Binding ProgressPixelWidth}"
```

If `ProgressPixelWidth` is not in the ViewModel yet, add it as a derived property updated from `ProgressRatio` and the measured progress track width in code-behind.

- [ ] **Step 3: Wire startup dependencies**

In `App.xaml.cs`, load config from `%APPDATA%\TerminalClockSpotify`, create logger, create `WindowsMediaSessionService`, create `MainViewModel`, set `MainWindow.DataContext`, and start timers after `MainWindow.Loaded`.

- [ ] **Step 4: Build UI**

Run:

```bash
rtk dotnet build src/TerminalClockSpotify/TerminalClockSpotify.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Commit layout**

```bash
rtk git add src/TerminalClockSpotify/App.xaml.cs src/TerminalClockSpotify/MainWindow.xaml src/TerminalClockSpotify/MainWindow.xaml.cs src/TerminalClockSpotify/ViewModels
rtk git commit -m "feat: build pixel terminal layout"
```

### Task 11: Window Behavior, Dock Snapping, and Context Menu

**Files:**
- Modify: `src/TerminalClockSpotify/MainWindow.xaml`
- Modify: `src/TerminalClockSpotify/MainWindow.xaml.cs`
- Modify: `src/TerminalClockSpotify/Config/ConfigLoader.cs`
- Modify: `src/TerminalClockSpotify/State/AppStateStore.cs`

- [ ] **Step 1: Implement desktop applet window flags**

In `MainWindow.xaml.cs`, use Win32 interop after source initialization to hide Alt-Tab entry, avoid focus on startup, and apply click-through only when config says `true`. Keep `ShowInTaskbar="False"` and `Topmost` bound to config.

- [ ] **Step 2: Implement drag snapping**

Handle `MouseLeftButtonDown`, `MouseMove`, and `MouseLeftButtonUp`. During drag, move the window for feedback. On release, call `PlacementService.SnapToNearest`, set `Left`, `Top`, `Width`, and `Height`, then save `PersistedAppState`.

- [ ] **Step 3: Implement context menu**

Add context menu items:

```xml
<MenuItem Header="Refresh" Click="Refresh_Click" />
<MenuItem Header="Config" Click="Config_Click" />
<MenuItem Header="Always On Top" IsCheckable="True" Click="AlwaysOnTop_Click" />
<Separator />
<MenuItem Header="Exit" Click="Exit_Click" />
```

`Refresh` reloads config and refreshes media. `Config` opens `%APPDATA%\TerminalClockSpotify\appsettings.json` with the shell. `Always On Top` toggles `Topmost` and persists config. `Exit` closes the app.

- [ ] **Step 4: Build behavior**

Run:

```bash
rtk dotnet build TerminalClockSpotify.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit applet behavior**

```bash
rtk git add src/TerminalClockSpotify
rtk git commit -m "feat: add applet window behavior"
```

### Task 12: Install and Uninstall Scripts

**Files:**
- Create: `scripts/install.ps1`
- Create: `scripts/uninstall.ps1`

- [ ] **Step 1: Write install script**

Create `scripts/install.ps1` that:

```powershell
$ErrorActionPreference = "Stop"
$runtime = dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App 8."
if (-not $runtime) {
  Write-Host "Install the .NET 8 Desktop Runtime, then rerun this script."
  exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $env:LOCALAPPDATA "TerminalClockSpotify\app"
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupDir "TerminalClockSpotify.lnk"

dotnet publish (Join-Path $repoRoot "src\TerminalClockSpotify\TerminalClockSpotify.csproj") -c Release -o $publishDir --self-contained false

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $publishDir "TerminalClockSpotify.exe"
$shortcut.WorkingDirectory = $publishDir
$shortcut.Save()

Write-Host "Installed TerminalClockSpotify to $publishDir"
Write-Host "Startup shortcut created at $shortcutPath"
```

- [ ] **Step 2: Write uninstall script**

Create `scripts/uninstall.ps1` that removes the startup shortcut and accepts `-RemoveAppFiles` to remove `%LOCALAPPDATA%\TerminalClockSpotify\app`.

- [ ] **Step 3: Validate PowerShell syntax**

Run:

```bash
rtk pwsh -NoProfile -Command "Get-Command ./scripts/install.ps1"
rtk pwsh -NoProfile -Command "Get-Command ./scripts/uninstall.ps1"
```

Expected: both scripts parse.

- [ ] **Step 4: Commit scripts**

```bash
rtk git add scripts
rtk git commit -m "feat: add startup install scripts"
```

### Task 13: Full Verification

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add README usage**

Create `README.md` with:

```markdown
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
```
```

- [ ] **Step 2: Run full automated verification**

Run:

```bash
rtk dotnet test TerminalClockSpotify.sln
rtk dotnet build TerminalClockSpotify.sln -c Release
```

Expected: tests pass and Release build succeeds.

- [ ] **Step 3: Run manual Windows verification**

On Windows 10 LTSC build 19044:

```powershell
dotnet run --project .\src\TerminalClockSpotify\TerminalClockSpotify.csproj
```

Verify:

- Applet opens without taskbar entry.
- Applet does not appear in Alt-Tab.
- Applet starts at the top-left Dock Position of Target Display `2` when available.
- Applet falls back to primary display when Target Display `2` is absent.
- Clock shows invariant `HH:mm` with leading zero.
- Spotify track metadata appears from local media session.
- Album art is rendered with nearest-neighbor pixel scaling.
- Closing Spotify shows `NO SPOTIFY SESSION` without resizing layout.
- Right-click `Refresh`, `Config`, `Always On Top`, and `Exit` work.

- [ ] **Step 4: Commit verification docs**

```bash
rtk git add README.md
rtk git commit -m "docs: add build and install usage"
```

## Self-Review

- Spec coverage: The plan covers WPF, media sessions, target display placement, Dock Positions, config defaults and refresh, runtime state, sharp pixel layout, album-art pixelation, low-frequency updates, startup scripts, error handling, and automated/manual tests.
- Scope: This is a single applet with one app and one test project. The WinRT probe is included before full UI verification because the spec requires proving local media-session behavior.
- Type consistency: `AppConfig`, `DisplayInfo`, `PlacementService`, `NowPlayingState`, `MediaSessionSelector`, `PixelArtRenderer`, and `MainViewModel` are introduced before later tasks consume them.
- Placeholder scan: No unresolved placeholder phrases or blank sections remain.
