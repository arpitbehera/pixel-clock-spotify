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
