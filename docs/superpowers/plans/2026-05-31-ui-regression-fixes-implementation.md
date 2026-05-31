# UI Regression Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix album-art placeholder bleed-through, stale five-second playback progress resets, and incorrect opposite-side window correction.

**Architecture:** Keep WPF visibility decisions bindable through `MainViewModel`, isolate WinRT timeline-age correction in a pure media helper, and adjust pure placement candidate ranking. Existing polling frequency, artwork rendering, and Win32 hook lifecycle remain unchanged.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit, Windows Media Control WinRT APIs

---

## File Structure

- Create `src/TerminalClockSpotify/Media/TimelinePositionNormalizer.cs`: pure timeline-age correction.
- Create `tests/TerminalClockSpotify.Tests/TimelinePositionNormalizerTests.cs`: portable timeline normalization coverage.
- Modify `src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs`: apply normalized WinRT timeline position.
- Modify `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`: expose artwork-presence binding.
- Modify `src/TerminalClockSpotify/MainWindow.xaml`: collapse fallback placeholder when artwork exists.
- Modify `tests/TerminalClockSpotify.Tests/MainViewModelTests.cs`: artwork-presence state regression.
- Modify `src/TerminalClockSpotify/Placement/SurroundingWindowLayoutPolicy.cs`: rank minimal-resize candidates by movement first.
- Modify `tests/TerminalClockSpotify.Tests/SurroundingWindowLayoutPolicyTests.cs`: below-applet regression.

### Task 1: Normalize Stale Windows Timeline Position

**Files:**
- Create: `src/TerminalClockSpotify/Media/TimelinePositionNormalizer.cs`
- Create: `tests/TerminalClockSpotify.Tests/TimelinePositionNormalizerTests.cs`
- Modify: `src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs`

- [ ] **Step 1: Write failing pure-helper tests**

```csharp
using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class TimelinePositionNormalizerTests
{
    private static readonly DateTimeOffset UpdatedAt = DateTimeOffset.Parse("2026-05-31T12:00:00Z");

    [Fact]
    public void PlayingPositionIncludesTimelineAge() =>
        Assert.Equal(
            TimeSpan.FromSeconds(15),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(100),
                MediaPlaybackKind.Playing,
                UpdatedAt,
                UpdatedAt.AddSeconds(5)));

    [Theory]
    [InlineData(MediaPlaybackKind.Paused)]
    [InlineData(MediaPlaybackKind.Stopped)]
    [InlineData(MediaPlaybackKind.Unknown)]
    public void NonPlayingPositionIgnoresTimelineAge(MediaPlaybackKind playbackKind) =>
        Assert.Equal(
            TimeSpan.FromSeconds(10),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(100),
                playbackKind,
                UpdatedAt,
                UpdatedAt.AddSeconds(5)));

    [Fact]
    public void NegativeTimelineAgeDoesNotMovePositionBackward() =>
        Assert.Equal(
            TimeSpan.FromSeconds(10),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(100),
                MediaPlaybackKind.Playing,
                UpdatedAt,
                UpdatedAt.AddSeconds(-5)));

    [Fact]
    public void PositionClampsToDuration() =>
        Assert.Equal(
            TimeSpan.FromSeconds(100),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(98),
                TimeSpan.FromSeconds(100),
                MediaPlaybackKind.Playing,
                UpdatedAt,
                UpdatedAt.AddSeconds(5)));
}
```

- [ ] **Step 2: Run focused tests; verify red**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter TimelinePositionNormalizerTests
```

Expected: FAIL because `TimelinePositionNormalizer` does not exist.

- [ ] **Step 3: Add pure helper**

```csharp
namespace TerminalClockSpotify.Media;

public static class TimelinePositionNormalizer
{
    public static TimeSpan Normalize(
        TimeSpan reportedPosition,
        TimeSpan duration,
        MediaPlaybackKind playbackKind,
        DateTimeOffset timelineLastUpdatedAt,
        DateTimeOffset observedAt)
    {
        var position = reportedPosition;
        if (playbackKind == MediaPlaybackKind.Playing && observedAt > timelineLastUpdatedAt)
            position += observedAt - timelineLastUpdatedAt;

        if (duration <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, duration.Ticks));
    }
}
```

- [ ] **Step 4: Apply helper at WinRT boundary**

In `WindowsMediaSessionService.GetNowPlayingAsync`, calculate normalized position after reading timeline:

```csharp
var position = TimelinePositionNormalizer.Normalize(
    timeline.Position,
    timeline.EndTime,
    playbackKind,
    timeline.LastUpdatedTime,
    DateTimeOffset.UtcNow);
```

Pass `position` instead of `timeline.Position` into `NowPlayingState`.

- [ ] **Step 5: Run tests and Windows build; verify green**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter TimelinePositionNormalizerTests
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build src/TerminalClockSpotify/TerminalClockSpotify.csproj -f net8.0-windows10.0.22621.0
```

Expected: PASS and successful build.

- [ ] **Step 6: Commit**

```bash
git add src/TerminalClockSpotify/Media/TimelinePositionNormalizer.cs src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs tests/TerminalClockSpotify.Tests/TimelinePositionNormalizerTests.cs
git commit -m "fix: normalize spotify timeline age"
```

### Task 2: Hide Placeholder Behind Real Artwork

**Files:**
- Modify: `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`
- Modify: `src/TerminalClockSpotify/MainWindow.xaml`
- Modify: `tests/TerminalClockSpotify.Tests/MainViewModelTests.cs`

- [ ] **Step 1: Extend artwork regression test**

In `ThumbnailBytesFlowIntoBindableArtworkAndMissingArtworkClearsIt`, add:

```csharp
Assert.True(viewModel.HasArtwork);
```

after the first refresh, and:

```csharp
Assert.False(viewModel.HasArtwork);
```

after the second refresh.

- [ ] **Step 2: Run focused test; verify red**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter ThumbnailBytesFlowIntoBindableArtworkAndMissingArtworkClearsIt
```

Expected: FAIL because `HasArtwork` does not exist.

- [ ] **Step 3: Add bindable artwork-presence state**

In `MainViewModel`, add:

```csharp
private bool _hasArtwork;
public bool HasArtwork => _hasArtwork;
```

In `RefreshMediaAsync`, replace direct artwork assignment with:

```csharp
var artworkImage = _artworkImageProvider?.GetArtworkImage(state.ThumbnailBytes);
Set(ref _artworkImage, artworkImage, nameof(ArtworkImage));
Set(ref _hasArtwork, artworkImage is not null, nameof(HasArtwork));
```

- [ ] **Step 4: Collapse placeholder when artwork exists**

In `MainWindow.xaml`, wrap existing fallback rectangle and canvas in a grid with trigger:

```xml
<Grid>
  <Grid.Style>
    <Style TargetType="Grid">
      <Setter Property="Visibility" Value="Visible" />
      <Style.Triggers>
        <DataTrigger Binding="{Binding HasArtwork}" Value="True">
          <Setter Property="Visibility" Value="Collapsed" />
        </DataTrigger>
      </Style.Triggers>
    </Style>
  </Grid.Style>
  <Rectangle Fill="#191919" />
  <Canvas Width="82" Height="82" HorizontalAlignment="Center" VerticalAlignment="Center">
    <Ellipse Width="82" Height="82" Fill="{DynamicResource DimGreenBrush}" />
    <Rectangle Canvas.Left="18" Canvas.Top="25" Width="46" Height="7" Fill="#191919" />
    <Rectangle Canvas.Left="14" Canvas.Top="39" Width="52" Height="7" Fill="#191919" />
    <Rectangle Canvas.Left="20" Canvas.Top="53" Width="42" Height="7" Fill="#191919" />
    <Rectangle Canvas.Left="14" Canvas.Top="22" Width="10" Height="4" Fill="#191919" />
    <Rectangle Canvas.Left="58" Canvas.Top="32" Width="10" Height="4" Fill="#191919" />
    <Rectangle Canvas.Left="12" Canvas.Top="36" Width="10" Height="4" Fill="#191919" />
    <Rectangle Canvas.Left="60" Canvas.Top="46" Width="10" Height="4" Fill="#191919" />
    <Rectangle Canvas.Left="18" Canvas.Top="50" Width="10" Height="4" Fill="#191919" />
    <Rectangle Canvas.Left="56" Canvas.Top="60" Width="10" Height="4" Fill="#191919" />
  </Canvas>
</Grid>
```

Keep bound artwork `Image` after fallback grid.

- [ ] **Step 5: Run focused test and Windows build; verify green**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter ThumbnailBytesFlowIntoBindableArtworkAndMissingArtworkClearsIt
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build src/TerminalClockSpotify/TerminalClockSpotify.csproj -f net8.0-windows10.0.22621.0
```

Expected: PASS and successful build.

- [ ] **Step 6: Commit**

```bash
git add src/TerminalClockSpotify/ViewModels/MainViewModel.cs src/TerminalClockSpotify/MainWindow.xaml tests/TerminalClockSpotify.Tests/MainViewModelTests.cs
git commit -m "fix: hide fallback behind album art"
```

### Task 3: Prefer Nearby Below-Applet Window Placement

**Files:**
- Modify: `src/TerminalClockSpotify/Placement/SurroundingWindowLayoutPolicy.cs`
- Modify: `tests/TerminalClockSpotify.Tests/SurroundingWindowLayoutPolicyTests.cs`

- [ ] **Step 1: Add failing policy regression**

```csharp
[Fact]
public void ChoosesNearbyResizedBelowCandidateOverDistantFullSizeSideCandidate()
{
    var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
        new AppBounds(0, 0, 300, 250),
        new AppBounds(0, 0, 1000, 600),
        "DISPLAY1",
        Window(new AppBounds(10, 200, 400, 400)));

    Assert.Equal(new AppBounds(10, 250, 400, 350), correction?.Bounds);
}
```

- [ ] **Step 2: Run focused test; verify red**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter ChoosesNearbyResizedBelowCandidateOverDistantFullSizeSideCandidate
```

Expected: FAIL because existing policy returns distant full-size right-side placement.

- [ ] **Step 3: Rank minimally resized candidates by movement first**

Replace preserved-first and resized-fallback blocks with:

```csharp
var correction = regions
    .Select(region => FitInside(window.Bounds, region, preserveSize: false))
    .OrderBy(candidate => MovementSquared(window.Bounds, candidate))
    .ThenBy(candidate => SizeReduction(window.Bounds, candidate))
    .First();

return new SurroundingWindowCorrection(correction);
```

- [ ] **Step 4: Run placement suite; verify green**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter SurroundingWindowLayoutPolicyTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/TerminalClockSpotify/Placement/SurroundingWindowLayoutPolicy.cs tests/TerminalClockSpotify.Tests/SurroundingWindowLayoutPolicyTests.cs
git commit -m "fix: prefer nearby window correction"
```

### Task 4: Full Verification

**Files:**
- No source changes expected.

- [ ] **Step 1: Run portable suite**

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 2: Run Windows-target build**

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build src/TerminalClockSpotify/TerminalClockSpotify.csproj -f net8.0-windows10.0.22621.0
```

Expected: build succeeds.

- [ ] **Step 3: Check whitespace**

```bash
git diff --check HEAD~3..HEAD
```

Expected: no output.

- [ ] **Step 4: Record manual Windows checks**

Verify manually on Windows after install:

1. Play Spotify track with pixelated art containing transparency; fallback motif is absent.
2. Watch elapsed playback for at least 15 seconds; text advances once per second without five-second stalls or backward jumps.
3. Snap a normal window below applet; window remains below and shrinks vertically when remaining height is insufficient.
