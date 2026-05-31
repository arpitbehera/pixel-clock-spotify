using TerminalClockSpotify.Config;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ConfigLoaderTests
{
    [Fact]
    public void LoadCreatesDefaultConfigWhenMissing()
    {
        using var root = TestDirectory.Create();
        var loader = new ConfigLoader(root.Path);

        var config = loader.Load();

        Assert.Equal("2", config.TargetDisplayLabel);
        Assert.Equal("top-left", config.DockPosition);
        Assert.True(config.AlwaysOnTop);
        Assert.Equal(0.50, config.WindowWidthRatio);
        Assert.Equal(0.25, config.WindowHeightRatio);
        Assert.Equal(1000, config.ClockUpdateIntervalMs);
        Assert.Equal(1000, config.ProgressUpdateIntervalMs);
        Assert.Equal(5000, config.MediaUpdateIntervalMs);
        Assert.True(File.Exists(Path.Combine(root.Path, "appsettings.json")));
    }

    [Fact]
    public void InvalidRatiosFallBackToDefaults()
    {
        using var root = TestDirectory.Create();
        File.WriteAllText(Path.Combine(root.Path, "appsettings.json"), """
        { "windowWidthRatio": 2.0, "windowHeightRatio": -1.0, "opacity": 4.0 }
        """);
        var loader = new ConfigLoader(root.Path);

        var config = loader.Load();

        Assert.Equal(0.50, config.WindowWidthRatio);
        Assert.Equal(0.25, config.WindowHeightRatio);
        Assert.Equal(0.80, config.Opacity);
    }

    [Fact]
    public void IntervalsBelowLowerBoundsFallBackToDefaults()
    {
        using var root = TestDirectory.Create();
        File.WriteAllText(Path.Combine(root.Path, "appsettings.json"), """
        {
          "clockUpdateIntervalMs": 249,
          "progressUpdateIntervalMs": 249,
          "mediaUpdateIntervalMs": 999
        }
        """);
        var loader = new ConfigLoader(root.Path);

        var config = loader.Load();

        Assert.Equal(1000, config.ClockUpdateIntervalMs);
        Assert.Equal(1000, config.ProgressUpdateIntervalMs);
        Assert.Equal(5000, config.MediaUpdateIntervalMs);
    }
}

internal sealed class TestDirectory : IDisposable
{
    public string Path { get; }

    private TestDirectory(string path) => Path = path;

    public static TestDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tcs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TestDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
