# UI Regression Fixes Design

## Goal

Fix three regressions in the responsive pixel UI:

- real album art must fully replace the Spotify-like placeholder
- playing progress must advance smoothly each second without five-second stalls or resets
- overlapping windows dragged below the applet must stay below it, shrinking vertically when needed instead of moving to the opposite side of the display

## Album Art

Keep the existing cached `32x32` pixel-art bitmap and nearest-neighbor WPF scaling.
Expose artwork presence from the view model or derive it in XAML. Show the generated
Spotify-like placeholder only when `ArtworkImage` is `null`. When real artwork is
present, hide the placeholder entirely so transparent or partially transparent
album pixels reveal only the album-art frame background.

Add a view-model regression test proving artwork presence changes when thumbnail
state changes.

## Playback Progress

Windows timeline `Position` is reported relative to the timeline snapshot's
`LastUpdatedTime`, not the moment the application finishes polling. Capture that
timestamp in the Windows media-session adapter and normalize a playing snapshot
to poll-application time before storing the view-model baseline:

`normalized position = reported position + max(0, applied wall time - timeline last-updated time)`

Clamp the normalized value to duration. Paused, stopped, and unknown snapshots
keep the reported position unchanged. The view model continues using its monotonic
baseline for lightweight one-second updates after each normalized snapshot is
applied. Wall-clock time is used only once at the Windows API boundary to account
for timeline snapshot age.

Keep the portable view model independent from WinRT by placing normalization in a
small pure helper or in the Windows adapter with pure helper coverage. Add tests
for stale playing snapshots, non-playing snapshots, negative clock differences,
and duration clamping.

## Surrounding Window Layout

Keep existing candidate regions: left, right, and below the applet. Evaluate both
size-preserving and minimally resized candidates for each valid region. Rank
candidates by movement distance first, then by size reduction. This makes a
nearby below placement win over a distant opposite-side placement even when the
below placement needs a vertical resize.

For below placement, retain the dragged window's horizontal position when it fits
the work area, place its top at the applet bottom, and reduce height only as much
as needed to fit the remaining work area.

Add policy regression coverage proving a nearby resized-below candidate beats a
distant full-size side candidate.

## Scope

Do not change polling frequency, artwork pixelation size, click-through behavior,
drag gestures, Windows hook lifecycle, or applet docking behavior.

## Verification

Run portable tests, Windows-target build, and `git diff --check`. Live Windows
manual verification remains required for rendered artwork, active Spotify
progress, and OS window snapping.
