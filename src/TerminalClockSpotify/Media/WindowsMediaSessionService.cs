using Windows.Storage.Streams;
using Windows.Media.Control;
using TerminalClockSpotify.Logging;

namespace TerminalClockSpotify.Media;

public sealed class WindowsMediaSessionService(RollingFileLogger logger) : IMediaSessionService
{
    private readonly HashSet<string> _loggedThumbnailReadFailures = [];

    public async Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken)
    {
        var selected = await SelectSessionAsync(spotifyTokens, cancellationToken);
        if (selected is null)
            return NowPlayingState.Idle("NO SPOTIFY SESSION");

        var (session, playbackKind) = selected.Value;
        var media = await session.TryGetMediaPropertiesAsync();
        var timeline = session.GetTimelineProperties();
        var title = string.IsNullOrWhiteSpace(media.Title) ? "UNKNOWN TRACK" : media.Title;
        var trackIdentity = $"{session.SourceAppUserModelId}|{title}|{media.Artist}|{media.AlbumTitle}";
        var thumbnailBytes = await ReadThumbnailBytesAsync(media.Thumbnail, trackIdentity);

        return new NowPlayingState(
            "NOW PLAYING ON SPOTIFY",
            title,
            media.Artist ?? string.Empty,
            media.AlbumTitle ?? string.Empty,
            timeline.Position,
            timeline.EndTime,
            playbackKind,
            thumbnailBytes);
    }

    public async Task<bool> TogglePlayPauseAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken)
    {
        var selected = await SelectSessionAsync(spotifyTokens, cancellationToken);
        return selected is not null && await selected.Value.Session.TryTogglePlayPauseAsync();
    }

    private static async Task<(GlobalSystemMediaTransportControlsSession Session, MediaPlaybackKind PlaybackKind)?> SelectSessionAsync(
        IReadOnlyList<string> spotifyTokens,
        CancellationToken cancellationToken)
    {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var managerSessions = manager.GetSessions().ToArray();
        var candidates = managerSessions
            .Select(session => new MediaSessionCandidate(
                session.SourceAppUserModelId,
                string.Empty,
                MapPlayback(session.GetPlaybackInfo().PlaybackStatus),
                DateTimeOffset.UtcNow))
            .ToArray();
        var selected = MediaSessionSelector.Select(candidates, spotifyTokens);
        if (selected is null)
            return null;

        var index = Array.IndexOf(candidates, selected);
        return (managerSessions[index], selected.PlaybackKind);
    }

    private async Task<byte[]?> ReadThumbnailBytesAsync(IRandomAccessStreamReference? thumbnail, string trackIdentity)
    {
        if (thumbnail is null)
            return null;

        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream.Size > int.MaxValue)
                throw new InvalidOperationException("Media thumbnail exceeds the supported size.");

            using var reader = new DataReader(stream.GetInputStreamAt(0));
            var bytes = new byte[(int)stream.Size];
            await reader.LoadAsync((uint)bytes.Length);
            reader.ReadBytes(bytes);
            return bytes;
        }
        catch (Exception exception)
        {
            if (_loggedThumbnailReadFailures.Add(trackIdentity))
                logger.Warn($"Failed to read media thumbnail for {trackIdentity}: {exception.Message}");
            return null;
        }
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
