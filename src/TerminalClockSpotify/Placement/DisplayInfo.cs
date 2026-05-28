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
