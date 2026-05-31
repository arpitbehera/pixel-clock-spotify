namespace TerminalClockSpotify.Placement;

public sealed class CoalescingWindowQueue<T> where T : notnull
{
    private readonly HashSet<T> _known = [];
    private readonly List<T> _pending = [];

    public void Enqueue(T item)
    {
        if (_known.Add(item))
            _pending.Add(item);
    }

    public IReadOnlyList<T> Drain()
    {
        var drained = _pending.ToArray();
        _known.Clear();
        _pending.Clear();
        return drained;
    }
}

public sealed record SurroundingWindowFacts(
    AppBounds Bounds,
    string DisplayDeviceName,
    bool IsVisibleApplicationWindow,
    bool IsMinimized,
    bool IsMaximized,
    bool IsFullscreen);

public sealed record SurroundingWindowCorrection(AppBounds Bounds);

public static class SurroundingWindowLayoutPolicy
{
    public static SurroundingWindowCorrection? ComputeCorrection(
        AppBounds appletBounds,
        AppBounds displayWorkArea,
        string appletDisplayDeviceName,
        SurroundingWindowFacts window)
    {
        if (!window.IsVisibleApplicationWindow
            || window.IsMinimized
            || window.IsMaximized
            || window.IsFullscreen
            || !string.Equals(window.DisplayDeviceName, appletDisplayDeviceName, StringComparison.OrdinalIgnoreCase)
            || !Overlaps(window.Bounds, appletBounds))
        {
            return null;
        }

        var regions = CandidateRegions(appletBounds, displayWorkArea).Where(region => region.Width > 0 && region.Height > 0).ToArray();
        if (regions.Length == 0)
            return null;

        var preserved = regions
            .Where(region => window.Bounds.Width <= region.Width && window.Bounds.Height <= region.Height)
            .Select(region => FitInside(window.Bounds, region, preserveSize: true))
            .OrderBy(candidate => MovementSquared(window.Bounds, candidate))
            .FirstOrDefault();

        if (preserved is not null)
            return new SurroundingWindowCorrection(preserved);

        var resized = regions
            .Select(region => FitInside(window.Bounds, region, preserveSize: false))
            .OrderBy(candidate => SizeReduction(window.Bounds, candidate))
            .ThenBy(candidate => MovementSquared(window.Bounds, candidate))
            .First();

        return new SurroundingWindowCorrection(resized);
    }

    private static IEnumerable<AppBounds> CandidateRegions(AppBounds applet, AppBounds workArea)
    {
        yield return new AppBounds(
            workArea.Left,
            workArea.Top,
            Math.Max(0, applet.Left - workArea.Left),
            workArea.Height);
        yield return new AppBounds(
            applet.Left + applet.Width,
            workArea.Top,
            Math.Max(0, workArea.Left + workArea.Width - (applet.Left + applet.Width)),
            workArea.Height);
        yield return new AppBounds(
            workArea.Left,
            applet.Top + applet.Height,
            workArea.Width,
            Math.Max(0, workArea.Top + workArea.Height - (applet.Top + applet.Height)));
    }

    private static AppBounds FitInside(AppBounds window, AppBounds region, bool preserveSize)
    {
        var width = preserveSize ? window.Width : Math.Min(window.Width, region.Width);
        var height = preserveSize ? window.Height : Math.Min(window.Height, region.Height);
        return new AppBounds(
            Clamp(window.Left, region.Left, region.Left + region.Width - width),
            Clamp(window.Top, region.Top, region.Top + region.Height - height),
            width,
            height);
    }

    private static bool Overlaps(AppBounds left, AppBounds right) =>
        left.Left < right.Left + right.Width
        && left.Left + left.Width > right.Left
        && left.Top < right.Top + right.Height
        && left.Top + left.Height > right.Top;

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);

    private static double MovementSquared(AppBounds original, AppBounds candidate)
    {
        var deltaX = candidate.Left - original.Left;
        var deltaY = candidate.Top - original.Top;
        return deltaX * deltaX + deltaY * deltaY;
    }

    private static double SizeReduction(AppBounds original, AppBounds candidate) =>
        original.Width - candidate.Width + original.Height - candidate.Height;
}
