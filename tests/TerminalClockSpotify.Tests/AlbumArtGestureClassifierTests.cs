using TerminalClockSpotify.Input;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class AlbumArtGestureClassifierTests
{
    [Fact]
    public void MovementBelowSystemThresholdIsClick()
    {
        var gesture = AlbumArtGestureClassifier.Classify(
            deltaX: 3,
            deltaY: 3,
            minimumHorizontalDragDistance: 4,
            minimumVerticalDragDistance: 4);

        Assert.Equal(AlbumArtGesture.Click, gesture);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(0, -4)]
    public void MovementAtEitherSystemThresholdIsDrag(double deltaX, double deltaY)
    {
        var gesture = AlbumArtGestureClassifier.Classify(
            deltaX,
            deltaY,
            minimumHorizontalDragDistance: 4,
            minimumVerticalDragDistance: 4);

        Assert.Equal(AlbumArtGesture.Drag, gesture);
    }
}
