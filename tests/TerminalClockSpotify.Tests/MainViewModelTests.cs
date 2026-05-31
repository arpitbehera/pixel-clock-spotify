using TerminalClockSpotify.Media;
using TerminalClockSpotify.ViewModels;
using TerminalClockSpotify.Art;
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

    [Fact]
    public async Task ThumbnailBytesFlowIntoBindableArtworkAndMissingArtworkClearsIt()
    {
        var expectedArtwork = new object();
        var artwork = new FakeArtworkImageProvider(expectedArtwork, null);
        var service = new FakeMediaSessionService(
            State(thumbnailBytes: [1, 2, 3]),
            State(thumbnailBytes: null));
        var viewModel = new MainViewModel(service, ["Spotify"], artworkImageProvider: artwork);

        await viewModel.RefreshMediaAsync(CancellationToken.None);
        Assert.Same(expectedArtwork, viewModel.ArtworkImage);
        Assert.True(viewModel.HasArtwork);
        Assert.Equal([1, 2, 3], artwork.Requests[0]);

        await viewModel.RefreshMediaAsync(CancellationToken.None);
        Assert.Null(viewModel.ArtworkImage);
        Assert.False(viewModel.HasArtwork);
    }

    [Fact]
    public async Task TogglePlayPauseDelegatesToMediaSessionService()
    {
        var service = new FakeMediaSessionService(State()) { ToggleResult = true };
        var viewModel = new MainViewModel(service, ["Spotify"]);

        var toggled = await viewModel.TogglePlayPauseAsync(CancellationToken.None);

        Assert.True(toggled);
        Assert.Equal(1, service.ToggleCalls);
    }

    private static NowPlayingState State(
        double position = 0,
        double duration = 100,
        MediaPlaybackKind playbackKind = MediaPlaybackKind.Stopped,
        byte[]? thumbnailBytes = null) =>
        new(
            "NOW PLAYING ON SPOTIFY",
            "Track",
            "Artist",
            "Album",
            TimeSpan.FromSeconds(position),
            TimeSpan.FromSeconds(duration),
            playbackKind,
            thumbnailBytes);

    private sealed class FakeMediaSessionService(params NowPlayingState[] states) : IMediaSessionService
    {
        private readonly Queue<NowPlayingState> _states = new(states);

        public bool ToggleResult { get; init; }
        public int ToggleCalls { get; private set; }

        public Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken) =>
            Task.FromResult(_states.Dequeue());

        public Task<bool> TogglePlayPauseAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken)
        {
            ToggleCalls++;
            return Task.FromResult(ToggleResult);
        }
    }

    private sealed class FakeArtworkImageProvider(params object?[] images) : IArtworkImageProvider
    {
        private readonly Queue<object?> _images = new(images);

        public List<byte[]?> Requests { get; } = [];

        public object? GetArtworkImage(byte[]? thumbnailBytes)
        {
            Requests.Add(thumbnailBytes);
            return _images.Dequeue();
        }
    }
}
