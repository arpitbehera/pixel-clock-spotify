using TerminalClockSpotify.Media;
using TerminalClockSpotify.ViewModels;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshMediaUpdatesBindableMetadata()
    {
        var service = new FakeMediaSessionService(new NowPlayingState(
            "NOW PLAYING ON SPOTIFY",
            "Time",
            "Pink Floyd",
            "The Dark Side of the Moon",
            TimeSpan.FromSeconds(203),
            TimeSpan.FromSeconds(413),
            MediaPlaybackKind.Playing,
            null));
        var viewModel = new MainViewModel(service, ["Spotify"]);

        await viewModel.RefreshMediaAsync(CancellationToken.None);

        Assert.Equal("Time", viewModel.Title);
        Assert.Equal("Pink Floyd", viewModel.Artist);
        Assert.Equal("The Dark Side of the Moon", viewModel.Album);
        Assert.Equal("3:23", viewModel.ElapsedText);
        Assert.Equal("6:53", viewModel.DurationText);
        Assert.Equal(203d / 413d, viewModel.ProgressRatio, precision: 4);
    }

    private sealed class FakeMediaSessionService(NowPlayingState state) : IMediaSessionService
    {
        public Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken) =>
            Task.FromResult(state);
    }
}
