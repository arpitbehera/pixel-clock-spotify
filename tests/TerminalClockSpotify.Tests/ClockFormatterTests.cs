using TerminalClockSpotify.Clock;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ClockFormatterTests
{
    [Theory]
    [InlineData(2026, 5, 28, 0, 5, "00:05")]
    [InlineData(2026, 5, 28, 21, 37, "21:37")]
    public void FormatUsesInvariantTwentyFourHourClock(int year, int month, int day, int hour, int minute, string expected)
    {
        var value = new DateTimeOffset(year, month, day, hour, minute, 44, TimeSpan.Zero);

        Assert.Equal(expected, ClockFormatter.Format(value));
    }
}
