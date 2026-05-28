using TerminalClockSpotify.State;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class AppStateStoreTests
{
    [Fact]
    public void SaveAndLoadRoundTripsDockPosition()
    {
        using var root = TestDirectory.Create();
        var store = new AppStateStore(root.Path);

        store.Save(new PersistedAppState("\\\\.\\DISPLAY2", "top-right"));

        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal("\\\\.\\DISPLAY2", loaded.DisplayDeviceName);
        Assert.Equal("top-right", loaded.DockPosition);
    }

    [Fact]
    public void LoadReturnsNullForMissingState()
    {
        using var root = TestDirectory.Create();
        var store = new AppStateStore(root.Path);

        Assert.Null(store.Load());
    }
}
