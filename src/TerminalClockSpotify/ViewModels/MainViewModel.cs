using System.ComponentModel;
using System.Runtime.CompilerServices;
using TerminalClockSpotify.Clock;
using TerminalClockSpotify.Media;

namespace TerminalClockSpotify.ViewModels;

public sealed class MainViewModel(IMediaSessionService mediaSessionService, IReadOnlyList<string> spotifyTokens) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClockText { get; private set; } = ClockFormatter.Format(DateTimeOffset.Now);
    public string Header { get; private set; } = "NOW PLAYING ON SPOTIFY";
    public string Title { get; private set; } = "NO SPOTIFY SESSION";
    public string Artist { get; private set; } = string.Empty;
    public string Album { get; private set; } = string.Empty;
    public string ElapsedText { get; private set; } = "0:00";
    public string DurationText { get; private set; } = "0:00";
    public double ProgressRatio { get; private set; }
    public double ProgressPixelWidth { get; private set; }
    public bool IsIdle { get; private set; } = true;

    public void RefreshClock(DateTimeOffset now)
    {
        ClockText = ClockFormatter.Format(now);
        OnPropertyChanged(nameof(ClockText));
    }

    public async Task RefreshMediaAsync(CancellationToken cancellationToken)
    {
        var state = await mediaSessionService.GetNowPlayingAsync(spotifyTokens, cancellationToken);
        Header = state.Header;
        Title = state.Title;
        Artist = state.Artist;
        Album = state.Album;
        ElapsedText = ProgressFormatter.Format(state.Position);
        DurationText = ProgressFormatter.Format(state.Duration);
        ProgressRatio = ProgressFormatter.Ratio(state.Position, state.Duration);
        IsIdle = state.PlaybackKind is MediaPlaybackKind.Stopped or MediaPlaybackKind.Unknown;

        OnPropertyChanged(string.Empty);
    }

    public void SetProgressTrackWidth(double width)
    {
        ProgressPixelWidth = Math.Max(0, width) * ProgressRatio;
        OnPropertyChanged(nameof(ProgressPixelWidth));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
