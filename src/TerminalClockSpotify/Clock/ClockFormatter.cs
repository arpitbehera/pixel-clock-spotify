using System.Globalization;

namespace TerminalClockSpotify.Clock;

public static class ClockFormatter
{
    public static string Format(DateTimeOffset value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);
}
