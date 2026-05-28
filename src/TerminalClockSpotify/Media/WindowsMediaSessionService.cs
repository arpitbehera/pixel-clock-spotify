using Windows.Media.Control;

namespace TerminalClockSpotify.Media;

public sealed class WindowsMediaSessionService : IMediaSessionService
{
    public async Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken)
    {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var managerSessions = manager.GetSessions();
        var sessions = managerSessions
            .Select(session => new MediaSessionCandidate(
                session.SourceAppUserModelId,
                string.Empty,
                MapPlayback(session.GetPlaybackInfo().PlaybackStatus),
                DateTimeOffset.UtcNow))
            .ToArray();

        var selected = MediaSessionSelector.Select(sessions, spotifyTokens);
        if (selected is null)
            return NowPlayingState.Idle("NO SPOTIFY SESSION");

        var session = managerSessions.First(s => s.SourceAppUserModelId == selected.SourceApplicationId);
        var media = await session.TryGetMediaPropertiesAsync();
        var timeline = session.GetTimelineProperties();

        return new NowPlayingState(
            "NOW PLAYING ON SPOTIFY",
            string.IsNullOrWhiteSpace(media.Title) ? "UNKNOWN TRACK" : media.Title,
            media.Artist ?? string.Empty,
            media.AlbumTitle ?? string.Empty,
            timeline.Position,
            timeline.EndTime,
            selected.PlaybackKind,
            null);
    }

    private static MediaPlaybackKind MapPlayback(GlobalSystemMediaTransportControlsSessionPlaybackStatus status) =>
        status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackKind.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackKind.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackKind.Stopped,
            _ => MediaPlaybackKind.Unknown
        };
}
