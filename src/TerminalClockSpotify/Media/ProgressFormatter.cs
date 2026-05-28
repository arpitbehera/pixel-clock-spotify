namespace TerminalClockSpotify.Media;

public static class ProgressFormatter
{
    public static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            value = TimeSpan.Zero;

        var totalSeconds = (int)Math.Floor(value.TotalSeconds);
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    public static double Ratio(TimeSpan position, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return 0.0;

        return Math.Clamp(position.TotalSeconds / duration.TotalSeconds, 0.0, 1.0);
    }
}
