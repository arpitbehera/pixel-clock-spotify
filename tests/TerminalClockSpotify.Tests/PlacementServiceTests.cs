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
