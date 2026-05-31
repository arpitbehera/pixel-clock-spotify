namespace TerminalClockSpotify.Media;

public static class MediaSessionSelector
{
    public static MediaSessionCandidate? Select(IReadOnlyList<MediaSessionCandidate> sessions, IReadOnlyList<string> spotifyTokens)
    {
        var matches = sessions
            .Where(session => spotifyTokens.Any(token => session.SourceApplicationId.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return matches
            .OrderBy(session => session.PlaybackKind switch
            {
                MediaPlaybackKind.Playing => 0,
                MediaPlaybackKind.Paused => 1,
                MediaPlaybackKind.Stopped => 2,
                _ => 3
            })
            .FirstOrDefault();
    }
}
