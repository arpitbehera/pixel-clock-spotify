# Album Art Watermark Crop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Do not spawn subagents for this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the Spotify watermark from rendered album art by cropping the bottom strip before pixelation.

**Architecture:** Keep the behavior in the existing artwork pipeline. `PixelArtRenderer` owns image-space transforms, and `AlbumArtBitmapAdapter` applies those transforms before producing the WPF `BitmapSource`.

**Tech Stack:** C# 12, .NET 8, WPF on `net8.0-windows10.0.22621.0`, xUnit tests on `net8.0`.

---

## File Structure

- Modify: `src/TerminalClockSpotify/Art/PixelArtRenderer.cs`
  - Add `CropBottom(BgraImage source, double heightRatio)` helper.
  - Clamp crop height so at least one source row remains.
- Modify: `src/TerminalClockSpotify/Art/AlbumArtBitmapAdapter.cs`
  - Crop bottom 15% before `Downsample(..., 32, 32)`.
- Modify: `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`
  - Add unit tests for bottom crop behavior and clamp behavior.

---

### Task 1: Add Crop Helper Tests

**Files:**
- Modify: `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`

- [ ] **Step 1: Write failing tests**

Append these tests inside `PixelArtRendererTests`:

```csharp
[Fact]
public void CropBottomRemovesBottomRows()
{
    var image = new BgraImage(2, 4, [
        new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
        new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
        new Bgra32(5, 0, 0, 255), new Bgra32(6, 0, 0, 255),
        new Bgra32(7, 0, 0, 255), new Bgra32(8, 0, 0, 255),
    ]);

    var result = PixelArtRenderer.CropBottom(image, 0.25);

    Assert.Equal(2, result.Width);
    Assert.Equal(3, result.Height);
    Assert.Equal([
        new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
        new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
        new Bgra32(5, 0, 0, 255), new Bgra32(6, 0, 0, 255),
    ], result.Pixels);
}

[Fact]
public void CropBottomLeavesAtLeastOneRow()
{
    var image = new BgraImage(2, 2, [
        new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
        new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
    ]);

    var result = PixelArtRenderer.CropBottom(image, 1);

    Assert.Equal(2, result.Width);
    Assert.Equal(1, result.Height);
    Assert.Equal([
        new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
    ], result.Pixels);
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter PixelArtRendererTests
```

Expected: FAIL because `PixelArtRenderer.CropBottom` does not exist.

---

### Task 2: Implement Crop Helper

**Files:**
- Modify: `src/TerminalClockSpotify/Art/PixelArtRenderer.cs`
- Test: `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`

- [ ] **Step 1: Add minimal implementation**

Add this method to `PixelArtRenderer`:

```csharp
public static BgraImage CropBottom(BgraImage source, double heightRatio)
{
    if (heightRatio <= 0)
        return source;

    var removedRows = (int)Math.Round(source.Height * heightRatio, MidpointRounding.AwayFromZero);
    var targetHeight = Math.Max(1, source.Height - removedRows);
    var pixels = source.Pixels.Take(source.Width * targetHeight).ToArray();

    return new BgraImage(source.Width, targetHeight, pixels);
}
```

- [ ] **Step 2: Run focused tests**

Run:

```bash
rtk dotnet test tests/TerminalClockSpotify.Tests/TerminalClockSpotify.Tests.csproj --filter PixelArtRendererTests
```

Expected: PASS.

---

### Task 3: Apply Crop Before Pixelation

**Files:**
- Modify: `src/TerminalClockSpotify/Art/AlbumArtBitmapAdapter.cs`
- Test: `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`

- [ ] **Step 1: Update adapter**

Replace this line:

```csharp
var pixelated = PixelArtRenderer.Downsample(new BgraImage(source.PixelWidth, source.PixelHeight, pixels), 32, 32);
```

With:

```csharp
var cropped = PixelArtRenderer.CropBottom(new BgraImage(source.PixelWidth, source.PixelHeight, pixels), 0.15);
var pixelated = PixelArtRenderer.Downsample(cropped, 32, 32);
```

- [ ] **Step 2: Run full tests**

Run:

```bash
rtk dotnet test TerminalClockSpotify.sln
```

Expected: PASS.

---

### Task 4: Commit Implementation

**Files:**
- Modify: `src/TerminalClockSpotify/Art/PixelArtRenderer.cs`
- Modify: `src/TerminalClockSpotify/Art/AlbumArtBitmapAdapter.cs`
- Modify: `tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs`

- [ ] **Step 1: Review diff**

Run:

```bash
rtk git diff -- src/TerminalClockSpotify/Art/PixelArtRenderer.cs src/TerminalClockSpotify/Art/AlbumArtBitmapAdapter.cs tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs
```

Expected: diff only contains crop helper, adapter crop call, and tests.

- [ ] **Step 2: Commit**

Run:

```bash
rtk git add src/TerminalClockSpotify/Art/PixelArtRenderer.cs src/TerminalClockSpotify/Art/AlbumArtBitmapAdapter.cs tests/TerminalClockSpotify.Tests/PixelArtRendererTests.cs
rtk git commit -m "fix: crop album art watermark"
```
