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

    public static AppBounds ComputeBounds(
        DisplayInfo display,
        string dockPosition,
        double widthRatio,
        double heightRatio,
        double? fixedWidth,
        double? fixedHeight)
    {
        var width = fixedWidth ?? display.Width * widthRatio;
        var height = fixedHeight ?? display.Height * heightRatio;
        var left = string.Equals(dockPosition, "top-right", StringComparison.OrdinalIgnoreCase)
            ? display.Left + display.Width - width
            : display.Left;

        return new AppBounds(left, display.Top, width, height);
    }

    public static DockTarget SnapToNearest(
        IReadOnlyList<DisplayInfo> displays,
        double pointerLeft,
        double pointerTop,
        double appWidth,
        double appHeight)
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
