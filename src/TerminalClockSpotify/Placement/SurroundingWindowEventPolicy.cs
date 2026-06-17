namespace TerminalClockSpotify.Placement;

/// <summary>
/// What the surrounding-window layout service should do in response to a WinEvent.
/// </summary>
public enum SurroundingWindowEventAction
{
    /// <summary>Event is noise (e.g. a location change mid-animation) and must be ignored.</summary>
    Ignore,

    /// <summary>A window settled into a new position/size or became visible; re-check overlap.</summary>
    Enforce,
}

/// <summary>
/// Decides which WinEvents trigger surrounding-window enforcement.
///
/// Deliberately excludes <c>EVENT_OBJECT_LOCATIONCHANGE</c> (0x800B): subscribing to it
/// system-wide is a firehose that fires for every object move/animation on the desktop,
/// floods the UI dispatcher, and is the root cause of the applet's unbounded CPU/RAM growth.
/// We only react once a window has finished moving/resizing, been restored, or shown.
/// </summary>
public static class SurroundingWindowEventPolicy
{
    public const uint EventSystemMoveSizeStart = 0x000A;
    public const uint EventSystemMoveSizeEnd = 0x000B;
    public const uint EventSystemMinimizeEnd = 0x0017;
    public const uint EventObjectShow = 0x8002;
    public const uint EventObjectLocationChange = 0x800B;

    /// <summary>The minimal set of WinEvents the service needs to hook.</summary>
    public static IReadOnlyList<uint> TriggerEvents { get; } =
        [EventSystemMoveSizeEnd, EventSystemMinimizeEnd, EventObjectShow];

    public static SurroundingWindowEventAction Classify(uint eventType) => eventType switch
    {
        EventSystemMoveSizeEnd => SurroundingWindowEventAction.Enforce,
        EventSystemMinimizeEnd => SurroundingWindowEventAction.Enforce,
        EventObjectShow => SurroundingWindowEventAction.Enforce,
        _ => SurroundingWindowEventAction.Ignore,
    };
}
