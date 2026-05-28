using System.IO;
using System.Text.Json;

namespace TerminalClockSpotify.Config;

public sealed class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path;

    public ConfigLoader(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);
        _path = Path.Combine(rootDirectory, "appsettings.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_path))
        {
            var defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path), Options) ?? new AppConfig();
            return Validate(config);
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(Validate(config), Options));
    }

    private static AppConfig Validate(AppConfig config)
    {
        var defaults = new AppConfig();

        return config with
        {
            TargetDisplayLabel = string.IsNullOrWhiteSpace(config.TargetDisplayLabel)
                ? defaults.TargetDisplayLabel
                : config.TargetDisplayLabel,
            DockPosition = IsDockPosition(config.DockPosition) ? config.DockPosition : defaults.DockPosition,
            SpotifySourceAppIdContains = config.SpotifySourceAppIdContains.Length == 0
                ? defaults.SpotifySourceAppIdContains
                : config.SpotifySourceAppIdContains,
            PlacementRetryIntervalMs = Positive(config.PlacementRetryIntervalMs)
                ? config.PlacementRetryIntervalMs
                : defaults.PlacementRetryIntervalMs,
            PlacementRetryLimitMs = Positive(config.PlacementRetryLimitMs)
                ? config.PlacementRetryLimitMs
                : defaults.PlacementRetryLimitMs,
            WindowWidthRatio = Ratio(config.WindowWidthRatio) ? config.WindowWidthRatio : defaults.WindowWidthRatio,
            WindowHeightRatio = Ratio(config.WindowHeightRatio) ? config.WindowHeightRatio : defaults.WindowHeightRatio,
            FixedWindowWidth = Positive(config.FixedWindowWidth) ? config.FixedWindowWidth : defaults.FixedWindowWidth,
            FixedWindowHeight = Positive(config.FixedWindowHeight) ? config.FixedWindowHeight : defaults.FixedWindowHeight,
            Opacity = Ratio(config.Opacity) ? config.Opacity : defaults.Opacity,
            ClockUpdateIntervalMs = Positive(config.ClockUpdateIntervalMs)
                ? config.ClockUpdateIntervalMs
                : defaults.ClockUpdateIntervalMs,
            MediaUpdateIntervalMs = Positive(config.MediaUpdateIntervalMs)
                ? config.MediaUpdateIntervalMs
                : defaults.MediaUpdateIntervalMs,
            StartupShortcutName = string.IsNullOrWhiteSpace(config.StartupShortcutName)
                ? defaults.StartupShortcutName
                : config.StartupShortcutName,
            Palette = config.Palette ?? defaults.Palette
        };
    }

    private static bool Ratio(double value) => value > 0.0 && value <= 1.0;

    private static bool Positive(int value) => value > 0;

    private static bool Positive(double? value) => value is > 0.0;

    private static bool IsDockPosition(string value) =>
        string.Equals(value, "top-left", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "top-right", StringComparison.OrdinalIgnoreCase);
}
