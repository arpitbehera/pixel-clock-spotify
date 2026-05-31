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

    [Fact]
    public async Task PlayingProgressAdvancesFromMonotonicBaselineAndClampsToDuration()
    {
        var elapsed = TimeSpan.Zero;
        var service = new FakeMediaSessionService(State(position: 98, duration: 100, MediaPlaybackKind.Playing));
        var viewModel = new MainViewModel(service, ["Spotify"], () => elapsed);
        await viewModel.RefreshMediaAsync(CancellationToken.None);

        elapsed = TimeSpan.FromSeconds(5);
        viewModel.RefreshProgress();

        Assert.Equal("1:40", viewModel.ElapsedText);
        Assert.Equal(1.0, viewModel.ProgressRatio);
    }

    [Theory]
    [InlineData(MediaPlaybackKind.Paused)]
    [InlineData(MediaPlaybackKind.Stopped)]
    [InlineData(MediaPlaybackKind.Unknown)]
    public async Task NonPlayingProgressDoesNotAdvance(MediaPlaybackKind playbackKind)
    {
        var elapsed = TimeSpan.Zero;
        var service = new FakeMediaSessionService(State(position: 10, duration: 100, playbackKind));
        var viewModel = new MainViewModel(service, ["Spotify"], () => elapsed);
        await viewModel.RefreshMediaAsync(CancellationToken.None);

        elapsed = TimeSpan.FromSeconds(30);
        viewModel.RefreshProgress();

        Assert.Equal("0:10", viewModel.ElapsedText);
        Assert.Equal(0.1, viewModel.ProgressRatio, precision: 4);
    }

    [Fact]
    public async Task LaterSnapshotResynchronizesProgressBaseline()
    {
        var elapsed = TimeSpan.Zero;
        var service = new FakeMediaSessionService(
            State(position: 10, duration: 100, MediaPlaybackKind.Playing),
            State(position: 40, duration: 100, MediaPlaybackKind.Playing));
        var viewModel = new MainViewModel(service, ["Spotify"], () => elapsed);
        await viewModel.RefreshMediaAsync(CancellationToken.None);
        elapsed = TimeSpan.FromSeconds(20);
        viewModel.RefreshProgress();

        await viewModel.RefreshMediaAsync(CancellationToken.None);
        elapsed = TimeSpan.FromSeconds(21);
        viewModel.RefreshProgress();

        Assert.Equal("0:41", viewModel.ElapsedText);
    }

    [Fact]
    public void ClockOnlyNotifiesWhenDisplayedMinuteChanges()
    {
        var service = new FakeMediaSessionService(State());
        var viewModel = new MainViewModel(service, ["Spotify"]);
        var changes = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        viewModel.RefreshClock(DateTimeOffset.Parse("2026-05-31T10:12:05Z"));
        changes.Clear();
        viewModel.RefreshClock(DateTimeOffset.Parse("2026-05-31T10:12:59Z"));
        viewModel.RefreshClock(DateTimeOffset.Parse("2026-05-31T10:13:00Z"));

        Assert.Equal([nameof(MainViewModel.ClockText)], changes);
    }

    [Fact]
    public async Task ProgressWidthOnlyNotifiesAfterHalfDipChange()
    {
        var elapsed = TimeSpan.Zero;
        var service = new FakeMediaSessionService(State(position: 10, duration: 100, MediaPlaybackKind.Playing));
        var viewModel = new MainViewModel(service, ["Spotify"], () => elapsed);
        await viewModel.RefreshMediaAsync(CancellationToken.None);
        viewModel.SetProgressTrackWidth(100);
        var changes = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        elapsed = TimeSpan.FromMilliseconds(400);
        viewModel.RefreshProgress();
        elapsed = TimeSpan.FromMilliseconds(600);
        viewModel.RefreshProgress();

        Assert.Single(changes, nameof(MainViewModel.ProgressPixelWidth));
        Assert.Equal(10.6, viewModel.ProgressPixelWidth, precision: 4);
    }

    private static NowPlayingState State(
        double position = 0,
        double duration = 100,
        MediaPlaybackKind playbackKind = MediaPlaybackKind.Stopped) =>
        new(
            "NOW PLAYING ON SPOTIFY",
            "Track",
            "Artist",
            "Album",
            TimeSpan.FromSeconds(position),
            TimeSpan.FromSeconds(duration),
            playbackKind,
            null);

    private sealed class FakeMediaSessionService(params NowPlayingState[] states) : IMediaSessionService
    {
        private readonly Queue<NowPlayingState> _states = new(states);

        public Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken) =>
            Task.FromResult(_states.Dequeue());
    }
}
