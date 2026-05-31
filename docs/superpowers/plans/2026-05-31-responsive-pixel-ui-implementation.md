# Responsive Pixel UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the WPF applet update progress responsively, render cached pixelated album art, toggle Spotify from the artwork frame, match the pixel UI reference more closely, and keep nearby application windows outside the docked applet rectangle.

**Architecture:** Keep Windows APIs at the application edge and move decisions into small pure services. Use one lightweight UI scheduler, a serialized media-poll coordinator, view-model monotonic progress baselines, a Windows-only cached artwork adapter, and a Windows-only surrounding-window event service backed by a pure layout policy.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit, Windows Media Control WinRT APIs, Win32 window hooks.

---

## File Structure

- Modify `src/TerminalClockSpotify/Config/AppConfig.cs`, `ConfigLoader.cs`, and `appsettings.default.json`: responsive interval defaults and lower-bound validation.
- Create `src/TerminalClockSpotify/Scheduling/LightweightUpdateScheduler.cs`: one-timer clock/progress due-time decisions.
- Create `src/TerminalClockSpotify/Media/SerializedMediaPollCoordinator.cs`: at-most-one active and one coalesced follow-up poll.
- Modify `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`: changed-only notifications, monotonic local progress, and bindable artwork.
- Modify `src/TerminalClockSpotify/Media/IMediaSessionService.cs`, `NowPlayingState.cs`, `MediaSessionSelector.cs`, and `WindowsMediaSessionService.cs`: shared selection policy, best-effort thumbnail reads, and toggle support.
- Create `src/TerminalClockSpotify/Art/IArtworkImageProvider.cs` and `AlbumArtBitmapAdapter.cs`: testable artwork boundary and Windows-only frozen bitmap cache.
- Modify `src/TerminalClockSpotify/MainWindow.xaml` and `MainWindow.xaml.cs`: packaged fonts, reference-inspired treatment, artwork binding, one lightweight timer, serialized polls, refresh reload, and click-versus-drag gestures.
- Add `src/TerminalClockSpotify/Assets/Fonts/*.ttf` and font license files: redistributable clock and media fonts.
- Create `src/TerminalClockSpotify/Placement/SurroundingWindowLayoutPolicy.cs` and `SurroundingWindowLayoutService.cs`: pure candidate selection and Windows event enforcement.
- Modify `src/TerminalClockSpotify/App.xaml.cs` and `TerminalClockSpotify.csproj`: assemble Windows-only adapters and exclude Windows-only files from the portable test target.

## Task 1: Responsive Timing Core

**Files:**
- Modify: `src/TerminalClockSpotify/Config/AppConfig.cs`
- Modify: `src/TerminalClockSpotify/Config/ConfigLoader.cs`
- Modify: `src/TerminalClockSpotify/appsettings.default.json`
- Create: `src/TerminalClockSpotify/Scheduling/LightweightUpdateScheduler.cs`
- Create: `src/TerminalClockSpotify/Media/SerializedMediaPollCoordinator.cs`
- Modify: `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`
- Test: `tests/TerminalClockSpotify.Tests/ConfigLoaderTests.cs`
- Create: `tests/TerminalClockSpotify.Tests/LightweightUpdateSchedulerTests.cs`
- Create: `tests/TerminalClockSpotify.Tests/SerializedMediaPollCoordinatorTests.cs`
- Modify: `tests/TerminalClockSpotify.Tests/MainViewModelTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for `progressUpdateIntervalMs = 1000`, `mediaUpdateIntervalMs = 5000`, lower-bound fallback, default one-second scheduler coalescing, non-default independent due intervals, one active plus one coalesced follow-up poll, monotonic playing progress, stationary non-playing progress, duration clamping, baseline resynchronization, and changed-only property notifications.

- [ ] **Step 2: Run tests to verify red**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj
```

Expected: FAIL because the new timing types and config property do not exist.

- [ ] **Step 3: Implement the minimal timing core**

Add `ProgressUpdateIntervalMs`, lower-bound validation, `LightweightUpdateScheduler.Tick(long elapsedMilliseconds)`, `SerializedMediaPollCoordinator.RequestAsync()`, and a view-model baseline based on an injected `Func<TimeSpan>` monotonic provider. Notify only values whose bindable representation changed, and only notify progress width after a `0.5` DIP change.

- [ ] **Step 4: Run tests to verify green**

Run the Task 1 test suite and expect PASS.

## Task 2: Spotify Artwork and Toggle Boundary

**Files:**
- Modify: `src/TerminalClockSpotify/Media/IMediaSessionService.cs`
- Modify: `src/TerminalClockSpotify/Media/MediaSessionSelector.cs`
- Modify: `src/TerminalClockSpotify/Media/WindowsMediaSessionService.cs`
- Create: `src/TerminalClockSpotify/Art/IArtworkImageProvider.cs`
- Create: `src/TerminalClockSpotify/Art/AlbumArtBitmapAdapter.cs`
- Modify: `src/TerminalClockSpotify/ViewModels/MainViewModel.cs`
- Modify: `src/TerminalClockSpotify/TerminalClockSpotify.csproj`
- Modify: `src/TerminalClockSpotify/App.xaml.cs`
- Modify: `tests/TerminalClockSpotify.Tests/MediaSessionSelectorTests.cs`
- Modify: `tests/TerminalClockSpotify.Tests/MainViewModelTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests proving selection order is playing, paused, stopped, unknown while preserving enumeration order, thumbnail bytes flow into artwork state, missing artwork clears prior artwork, unchanged thumbnail content reuses cached bindable state, and toggle delegates to the media boundary.

- [ ] **Step 2: Run tests to verify red**

Run the focused selector and view-model tests. Expect FAIL for missing toggle and artwork APIs.

- [ ] **Step 3: Implement the minimal boundary**

Extend `IMediaSessionService` with toggle support. Read thumbnail bytes best-effort in the Windows adapter, add hash-based decode caching in a Windows-only `AlbumArtBitmapAdapter`, freeze the generated `32x32` bitmap, and expose nullable `ArtworkImage` from the view model.

- [ ] **Step 4: Run tests to verify green**

Run the full portable suite and a Windows-target build. Expect PASS.

## Task 3: Pixel UI and Artwork Gesture Integration

**Files:**
- Modify: `src/TerminalClockSpotify/MainWindow.xaml`
- Modify: `src/TerminalClockSpotify/MainWindow.xaml.cs`
- Create: `src/TerminalClockSpotify/Input/AlbumArtGestureClassifier.cs`
- Create: `tests/TerminalClockSpotify.Tests/AlbumArtGestureClassifierTests.cs`
- Add: `src/TerminalClockSpotify/Assets/Fonts/*.ttf`
- Add: `src/TerminalClockSpotify/Assets/Fonts/*-LICENSE.txt`

- [ ] **Step 1: Write failing gesture tests**

Test that movement below system thresholds is a click and movement beyond either threshold is a drag.

- [ ] **Step 2: Run tests to verify red**

Run the gesture tests. Expect FAIL because the classifier is absent.

- [ ] **Step 3: Implement the WPF integration**

Replace both existing timers with one lightweight scheduler timer plus the serialized media poll timer. Restart timers on refresh. Bind artwork above a generated green-circle-and-wave placeholder, use nearest-neighbor rendering, add the centered header lines, bundle separate packaged clock and media fonts with `Consolas` fallback, and classify artwork pointer input before toggling or dragging.

- [ ] **Step 4: Run tests and build**

Run the portable tests and Windows-target build. Expect PASS.

## Task 4: Surrounding Window Layout

**Files:**
- Create: `src/TerminalClockSpotify/Placement/SurroundingWindowLayoutPolicy.cs`
- Create: `src/TerminalClockSpotify/Placement/SurroundingWindowLayoutService.cs`
- Modify: `src/TerminalClockSpotify/TerminalClockSpotify.csproj`
- Modify: `src/TerminalClockSpotify/MainWindow.xaml.cs`
- Create: `tests/TerminalClockSpotify.Tests/SurroundingWindowLayoutPolicyTests.cs`

- [ ] **Step 1: Write failing policy tests**

Test overlap detection, same-display filtering, maximized ignore behavior, smallest valid beside-or-below movement, size preservation when possible, and minimal resize fallback.

- [ ] **Step 2: Run tests to verify red**

Run the policy tests. Expect FAIL because the policy is absent.

- [ ] **Step 3: Implement pure layout policy**

Add immutable window facts and a correction result. Return `null` for ignored or non-overlapping windows. For overlaps, compare valid beside and below candidates by movement distance and minimally resize only when neither candidate fits.

- [ ] **Step 4: Implement Windows event service**

Subscribe with `SetWinEventHook`, coalesce changed-window callbacks, skip own-process callbacks, suppress self-induced position events, enumerate only after relevant applet changes, and disable hooks when click-through is active.

- [ ] **Step 5: Run tests and build**

Run the portable tests and Windows-target build. Expect PASS.

## Task 5: Verification

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Document runtime behavior**

Update the README with responsive progress, five-second media polling, artwork toggle, click-through behavior, surrounding-window exclusion, and the manual Windows verification checklist.

- [ ] **Step 2: Run full verification**

Run:

```bash
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test TerminalClockSpotify.sln
PATH="$HOME/.dotnet:$PATH" DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build src/TerminalClockSpotify/TerminalClockSpotify.csproj -f net8.0-windows10.0.22621.0
git diff --check
```

Expected: all tests pass, Windows target builds, and no whitespace errors are reported.

- [ ] **Step 3: Review manual-only risk**

Record that live Spotify media control, WPF font appearance, artwork rendering, drag snapping, click-through, and Win32 surrounding-window behavior require manual verification on Windows.
