using TerminalClockSpotify.Logging;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class RollingFileLoggerTests
{
    [Fact]
    public void LoggerRetainsConfiguredNumberOfFiles()
    {
        using var root = TestDirectory.Create();
        var logger = new RollingFileLogger(root.Path, maxBytes: 4096, retainedFiles: 3);

        for (var i = 0; i < 20; i++)
            logger.Info(new string('x', 2048));

        var files = Directory.GetFiles(root.Path, "app*.log");
        Assert.InRange(files.Length, 1, 3);
    }
}
