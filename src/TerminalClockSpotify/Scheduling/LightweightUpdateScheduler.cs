namespace TerminalClockSpotify.Scheduling;

public readonly record struct LightweightUpdates(bool Clock, bool Progress);

public sealed class LightweightUpdateScheduler
{
    private readonly int _clockIntervalMs;
    private readonly int _progressIntervalMs;
    private long _nextClockUpdateMs;
    private long _nextProgressUpdateMs;

    public LightweightUpdateScheduler(int clockIntervalMs, int progressIntervalMs)
    {
        _clockIntervalMs = clockIntervalMs;
        _progressIntervalMs = progressIntervalMs;
        _nextClockUpdateMs = clockIntervalMs;
        _nextProgressUpdateMs = progressIntervalMs;
    }

    public int TimerIntervalMs => Math.Min(_clockIntervalMs, _progressIntervalMs);

    public LightweightUpdates Tick(long elapsedMilliseconds)
    {
        var clockDue = elapsedMilliseconds >= _nextClockUpdateMs;
        var progressDue = elapsedMilliseconds >= _nextProgressUpdateMs;

        if (clockDue)
            _nextClockUpdateMs = NextDueAfter(elapsedMilliseconds, _clockIntervalMs);
        if (progressDue)
            _nextProgressUpdateMs = NextDueAfter(elapsedMilliseconds, _progressIntervalMs);

        return new LightweightUpdates(clockDue, progressDue);
    }

    private static long NextDueAfter(long elapsedMilliseconds, int intervalMs) =>
        (elapsedMilliseconds / intervalMs + 1) * intervalMs;
}
