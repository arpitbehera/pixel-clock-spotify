namespace TerminalClockSpotify.Media;

public static class TimelinePositionNormalizer
{
    public static TimeSpan Normalize(
        TimeSpan reportedPosition,
        TimeSpan duration,
        MediaPlaybackKind playbackKind,
        DateTimeOffset timelineLastUpdatedAt,
        DateTimeOffset observedAt)
    {
        var position = reportedPosition;
        if (playbackKind == MediaPlaybackKind.Playing && observedAt > timelineLastUpdatedAt)
            position += observedAt - timelineLastUpdatedAt;

        if (duration <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, duration.Ticks));
    }
}
