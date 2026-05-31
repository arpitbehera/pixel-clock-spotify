using TerminalClockSpotify.Scheduling;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class LightweightUpdateSchedulerTests
{
    [Fact]
    public void DefaultIntervalsShareOneWakePerSecond()
    {
        var scheduler = new LightweightUpdateScheduler(clockIntervalMs: 1000, progressIntervalMs: 1000);

        Assert.Equal(new LightweightUpdates(false, false), scheduler.Tick(999));
        Assert.Equal(new LightweightUpdates(true, true), scheduler.Tick(1000));
        Assert.Equal(new LightweightUpdates(false, false), scheduler.Tick(1999));
        Assert.Equal(new LightweightUpdates(true, true), scheduler.Tick(2000));
    }

    [Fact]
    public void DifferentIntervalsBecomeDueIndependently()
    {
        var scheduler = new LightweightUpdateScheduler(clockIntervalMs: 1000, progressIntervalMs: 250);

        Assert.Equal(250, scheduler.TimerIntervalMs);
        Assert.Equal(new LightweightUpdates(false, true), scheduler.Tick(250));
        Assert.Equal(new LightweightUpdates(false, true), scheduler.Tick(500));
        Assert.Equal(new LightweightUpdates(true, true), scheduler.Tick(1000));
    }
}
