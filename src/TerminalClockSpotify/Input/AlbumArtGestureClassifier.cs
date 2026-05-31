namespace TerminalClockSpotify.Input;

public enum AlbumArtGesture
{
    Click,
    Drag
}

public static class AlbumArtGestureClassifier
{
    public static AlbumArtGesture Classify(
        double deltaX,
        double deltaY,
        double minimumHorizontalDragDistance,
        double minimumVerticalDragDistance) =>
        Math.Abs(deltaX) >= minimumHorizontalDragDistance
        || Math.Abs(deltaY) >= minimumVerticalDragDistance
            ? AlbumArtGesture.Drag
            : AlbumArtGesture.Click;
}
