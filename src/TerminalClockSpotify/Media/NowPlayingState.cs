namespace TerminalClockSpotify.Media;

public enum MediaPlaybackKind
{
    Unknown,
    Playing,
    Paused,
    Stopped
}

public sealed record MediaSessionCandidate(
    string SourceApplicationId,
    string Title,
    MediaPlaybackKind PlaybackKind,
    DateTimeOffset LastUpdated);

public sealed record NowPlayingState(
    string Header,
    string Title,
    string Artist,
    string Album,
    TimeSpan Position,
    TimeSpan Duration,
    MediaPlaybackKind PlaybackKind,
    byte[]? ThumbnailBytes)
{
    public static NowPlayingState Idle(string message) =>
        new("NOW PLAYING ON SPOTIFY", message, string.Empty, string.Empty, TimeSpan.Zero, TimeSpan.Zero, MediaPlaybackKind.Stopped, null);
}
