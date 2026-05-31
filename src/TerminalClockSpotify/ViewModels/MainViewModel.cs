using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerminalClockSpotify.Art;
using TerminalClockSpotify.Clock;
using TerminalClockSpotify.Media;

namespace TerminalClockSpotify.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IMediaSessionService _mediaSessionService;
    private readonly IReadOnlyList<string> _spotifyTokens;
    private readonly Func<TimeSpan> _elapsedTimeProvider;
    private readonly IArtworkImageProvider? _artworkImageProvider;
    private TimeSpan _reportedPosition;
    private TimeSpan _duration;
    private TimeSpan _baselineAppliedAt;
    private MediaPlaybackKind _playbackKind;
    private double _progressTrackWidth;
    private string _clockText = ClockFormatter.Format(DateTimeOffset.Now);
    private string _header = "NOW PLAYING ON SPOTIFY";
    private string _title = "NO SPOTIFY SESSION";
    private string _artist = string.Empty;
    private string _album = string.Empty;
    private string _elapsedText = "0:00";
    private string _durationText = "0:00";
    private double _progressRatio;
    private double _progressPixelWidth;
    private bool _isIdle = true;
    private object? _artworkImage;

    public MainViewModel(
        IMediaSessionService mediaSessionService,
        IReadOnlyList<string> spotifyTokens,
        Func<TimeSpan>? elapsedTimeProvider = null,
        IArtworkImageProvider? artworkImageProvider = null)
    {
        _mediaSessionService = mediaSessionService;
        _spotifyTokens = spotifyTokens;
        _elapsedTimeProvider = elapsedTimeProvider ?? (() => Stopwatch.GetElapsedTime(0));
        _artworkImageProvider = artworkImageProvider;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClockText => _clockText;
    public string Header => _header;
    public string Title => _title;
    public string Artist => _artist;
    public string Album => _album;
    public string ElapsedText => _elapsedText;
    public string DurationText => _durationText;
    public double ProgressRatio => _progressRatio;
    public double ProgressPixelWidth => _progressPixelWidth;
    public bool IsIdle => _isIdle;
    public object? ArtworkImage => _artworkImage;

    public void RefreshClock(DateTimeOffset now)
    {
        Set(ref _clockText, ClockFormatter.Format(now), nameof(ClockText));
    }

    public async Task RefreshMediaAsync(CancellationToken cancellationToken)
    {
        var state = await _mediaSessionService.GetNowPlayingAsync(_spotifyTokens, cancellationToken);
        Set(ref _header, state.Header, nameof(Header));
        Set(ref _title, state.Title, nameof(Title));
        Set(ref _artist, state.Artist, nameof(Artist));
        Set(ref _album, state.Album, nameof(Album));
        Set(ref _durationText, ProgressFormatter.Format(state.Duration), nameof(DurationText));
        Set(ref _isIdle, state.PlaybackKind is MediaPlaybackKind.Stopped or MediaPlaybackKind.Unknown, nameof(IsIdle));
        Set(ref _artworkImage, _artworkImageProvider?.GetArtworkImage(state.ThumbnailBytes), nameof(ArtworkImage));

        _reportedPosition = state.Position;
        _duration = state.Duration;
        _playbackKind = state.PlaybackKind;
        _baselineAppliedAt = _elapsedTimeProvider();
        RefreshProgress();
    }

    public Task<bool> TogglePlayPauseAsync(CancellationToken cancellationToken) =>
        _mediaSessionService.TogglePlayPauseAsync(_spotifyTokens, cancellationToken);

    public void RefreshProgress()
    {
        var position = _reportedPosition;
        if (_playbackKind == MediaPlaybackKind.Playing)
            position += _elapsedTimeProvider() - _baselineAppliedAt;

        if (_duration <= TimeSpan.Zero)
            position = TimeSpan.Zero;
        else
            position = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, _duration.Ticks));

        Set(ref _elapsedText, ProgressFormatter.Format(position), nameof(ElapsedText));
        Set(ref _progressRatio, ProgressFormatter.Ratio(position, _duration), nameof(ProgressRatio));
        SetProgressPixelWidth(_progressTrackWidth * ProgressRatio);
    }

    public void SetProgressTrackWidth(double width)
    {
        _progressTrackWidth = Math.Max(0, width);
        SetProgressPixelWidth(_progressTrackWidth * ProgressRatio);
    }

    private void SetProgressPixelWidth(double value)
    {
        if (Math.Abs(_progressPixelWidth - value) < 0.5 && !(value == 0 && _progressPixelWidth != 0))
            return;

        _progressPixelWidth = value;
        OnPropertyChanged(nameof(ProgressPixelWidth));
    }

    private void Set<T>(ref T property, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(property, value))
            return;

        property = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
