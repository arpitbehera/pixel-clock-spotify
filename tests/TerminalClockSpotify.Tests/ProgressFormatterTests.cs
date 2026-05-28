using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ProgressFormatterTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(203, "3:23")]
    [InlineData(413, "6:53")]
    [InlineData(3605, "60:05")]
    public void FormatDurationUsesMinuteSecondDisplay(int seconds, string expected)
    {
        Assert.Equal(expected, ProgressFormatter.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(-5, 100, 0.0)]
    [InlineData(50, 100, 0.5)]
    [InlineData(125, 100, 1.0)]
    [InlineData(30, 0, 0.0)]
    public void RatioClampsToProgressRange(double position, double duration, double expected)
    {
        Assert.Equal(expected, ProgressFormatter.Ratio(TimeSpan.FromSeconds(position), TimeSpan.FromSeconds(duration)), precision: 4);
    }
}
