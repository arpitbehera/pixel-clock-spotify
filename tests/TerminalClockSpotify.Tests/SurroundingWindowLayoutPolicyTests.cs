using TerminalClockSpotify.Placement;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class SurroundingWindowLayoutPolicyTests
{
    private static readonly AppBounds WorkArea = new(0, 0, 1000, 800);
    private static readonly AppBounds Applet = new(0, 0, 300, 200);

    [Fact]
    public void NonOverlappingWindowNeedsNoCorrection()
    {
        var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
            Applet,
            WorkArea,
            "DISPLAY1",
            Window(new AppBounds(400, 300, 300, 200)));

        Assert.Null(correction);
    }

    [Fact]
    public void WindowOnAnotherDisplayNeedsNoCorrection()
    {
        var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
            Applet,
            WorkArea,
            "DISPLAY1",
            Window(new AppBounds(100, 100, 300, 200)) with { DisplayDeviceName = "DISPLAY2" });

        Assert.Null(correction);
    }

    [Fact]
    public void MaximizedWindowNeedsNoCorrection()
    {
        var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
            Applet,
            WorkArea,
            "DISPLAY1",
            Window(new AppBounds(100, 100, 300, 200)) with { IsMaximized = true });

        Assert.Null(correction);
    }

    [Fact]
    public void ChoosesSmallestMovementThatPreservesSize()
    {
        var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
            Applet,
            WorkArea,
            "DISPLAY1",
            Window(new AppBounds(250, 100, 400, 300)));

        Assert.Equal(new AppBounds(300, 100, 400, 300), correction?.Bounds);
    }

    [Fact]
    public void ChoosesBelowAppletWhenThatRequiresLessMovement()
    {
        var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
            new AppBounds(0, 0, 200, 200),
            WorkArea,
            "DISPLAY1",
            Window(new AppBounds(0, 150, 400, 300)));

        Assert.Equal(new AppBounds(0, 200, 400, 300), correction?.Bounds);
    }

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

    [Fact]
    public void ResizesMinimallyWhenNoFullSizeCandidateFits()
    {
        var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(
            new AppBounds(0, 0, 300, 250),
            new AppBounds(0, 0, 500, 400),
            "DISPLAY1",
            Window(new AppBounds(100, 100, 450, 350)));

        Assert.Equal(new AppBounds(50, 250, 450, 150), correction?.Bounds);
    }

    private static SurroundingWindowFacts Window(AppBounds bounds) =>
        new(
            Bounds: bounds,
            DisplayDeviceName: "DISPLAY1",
            IsVisibleApplicationWindow: true,
            IsMinimized: false,
            IsMaximized: false,
            IsFullscreen: false);

    [Fact]
    public void CoalescingQueueReturnsChangedWindowOncePerDrain()
    {
        var queue = new CoalescingWindowQueue<nint>();

        queue.Enqueue(42);
        queue.Enqueue(42);
        queue.Enqueue(84);

        Assert.Equal([(nint)42, (nint)84], queue.Drain());
        Assert.Empty(queue.Drain());
    }
}
