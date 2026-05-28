namespace TerminalClockSpotify.Media;

public static class MediaSessionSelector
{
    public static MediaSessionCandidate? Select(IReadOnlyList<MediaSessionCandidate> sessions, IReadOnlyList<string> spotifyTokens)
    {
        var matches = sessions
            .Where(session => spotifyTokens.Any(token => session.SourceApplicationId.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return matches
            .OrderByDescending(session => session.PlaybackKind == MediaPlaybackKind.Playing)
            .ThenByDescending(session => session.LastUpdated)
            .FirstOrDefault();
    }
}
