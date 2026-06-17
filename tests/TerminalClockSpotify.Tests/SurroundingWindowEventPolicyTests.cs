using TerminalClockSpotify.Placement;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class SurroundingWindowEventPolicyTests
{
    [Fact]
    public void LocationChangeIsIgnored()
    {
        // Regression guard: the system-wide EVENT_OBJECT_LOCATIONCHANGE firehose was the
        // root cause of unbounded CPU/RAM growth. It must never trigger enforcement.
        Assert.Equal(
            SurroundingWindowEventAction.Ignore,
            SurroundingWindowEventPolicy.Classify(SurroundingWindowEventPolicy.EventObjectLocationChange));
    }

    [Fact]
    public void MoveSizeStartIsIgnored()
    {
        Assert.Equal(
            SurroundingWindowEventAction.Ignore,
            SurroundingWindowEventPolicy.Classify(SurroundingWindowEventPolicy.EventSystemMoveSizeStart));
    }

    [Theory]
    [InlineData(SurroundingWindowEventPolicy.EventSystemMoveSizeEnd)]
    [InlineData(SurroundingWindowEventPolicy.EventSystemMinimizeEnd)]
    [InlineData(SurroundingWindowEventPolicy.EventObjectShow)]
    public void SettledWindowEventsEnforce(uint eventType)
    {
        Assert.Equal(SurroundingWindowEventAction.Enforce, SurroundingWindowEventPolicy.Classify(eventType));
    }

    [Fact]
    public void TriggerEventsExcludeLocationChangeAndMoveStart()
    {
        Assert.DoesNotContain(SurroundingWindowEventPolicy.EventObjectLocationChange, SurroundingWindowEventPolicy.TriggerEvents);
        Assert.DoesNotContain(SurroundingWindowEventPolicy.EventSystemMoveSizeStart, SurroundingWindowEventPolicy.TriggerEvents);
    }
}
