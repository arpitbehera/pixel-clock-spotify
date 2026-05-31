using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using TerminalClockSpotify.Logging;

namespace TerminalClockSpotify.Placement;

public sealed class SurroundingWindowLayoutService : IDisposable
{
    private readonly IntPtr _appletHwnd;
    private readonly RollingFileLogger _logger;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _coalesceTimer;
    private readonly WinEventDelegate _winEventDelegate;
    private readonly CoalescingWindowQueue<IntPtr> _pendingWindows = new();
    private readonly HashSet<IntPtr> _movingWindows = [];
    private readonly Dictionary<IntPtr, DateTimeOffset> _selfInducedMoves = [];
    private readonly List<IntPtr> _hooks = [];

    public SurroundingWindowLayoutService(IntPtr appletHwnd, RollingFileLogger logger)
    {
        _appletHwnd = appletHwnd;
        _logger = logger;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _winEventDelegate = HandleWinEvent;
        _coalesceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _coalesceTimer.Tick += (_, _) => ProcessPendingWindows();
    }

    public void Update(bool clickThrough)
    {
        if (clickThrough)
        {
            RemoveHooks();
            return;
        }

        EnsureHooks();
        EnforceAllWindows();
    }

    public void Dispose()
    {
        RemoveHooks();
        _coalesceTimer.Stop();
    }

    private void EnsureHooks()
    {
        if (_hooks.Count > 0)
            return;

        AddHook(EventSystemMoveSizeStart, EventSystemMinimizeEnd);
        AddHook(EventObjectShow, EventObjectLocationChange);
    }

    private void AddHook(uint eventMinimum, uint eventMaximum)
    {
        var hook = SetWinEventHook(
            eventMinimum,
            eventMaximum,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WinEventOutOfContext | WinEventSkipOwnProcess);
        if (hook == IntPtr.Zero)
            _logger.Warn($"Failed to subscribe to WinEvent range {eventMinimum:X}-{eventMaximum:X}.");
        else
            _hooks.Add(hook);
    }

    private void RemoveHooks()
    {
        foreach (var hook in _hooks)
            UnhookWinEvent(hook);

        _hooks.Clear();
        _movingWindows.Clear();
        _pendingWindows.Drain();
        _selfInducedMoves.Clear();
        _coalesceTimer.Stop();
    }

    private void HandleWinEvent(
        IntPtr hook,
        uint eventType,
        IntPtr hwnd,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime)
    {
        if (hwnd == IntPtr.Zero || (eventType >= EventObjectShow && objectId != ObjIdWindow))
            return;

        _dispatcher.BeginInvoke(() =>
        {
            if (eventType == EventSystemMoveSizeStart)
            {
                _movingWindows.Add(hwnd);
                return;
            }

            if (eventType == EventSystemMoveSizeEnd)
                _movingWindows.Remove(hwnd);
            else if (eventType == EventObjectLocationChange && _movingWindows.Contains(hwnd))
                return;
            else if (eventType is not EventSystemMinimizeEnd and not EventObjectShow and not EventObjectLocationChange)
                return;

            QueueWindow(hwnd);
        });
    }

    private void QueueWindow(IntPtr hwnd)
    {
        if (_selfInducedMoves.Remove(hwnd, out var movedAt)
            && DateTimeOffset.UtcNow - movedAt < TimeSpan.FromSeconds(1))
        {
            return;
        }

        _pendingWindows.Enqueue(hwnd);
        if (!_coalesceTimer.IsEnabled)
            _coalesceTimer.Start();
    }

    private void ProcessPendingWindows()
    {
        _coalesceTimer.Stop();
        foreach (var hwnd in _pendingWindows.Drain())
            EnforceWindow(hwnd);
    }

    private void EnforceAllWindows()
    {
        EnumWindows((hwnd, _) =>
        {
            EnforceWindow(hwnd);
            return true;
        }, IntPtr.Zero);
    }

    private void EnforceWindow(IntPtr hwnd)
    {
        try
        {
            if (!TryGetAppletDisplay(out var appletBounds, out var workArea, out var appletDisplay, out var appletMonitor)
                || !TryGetWindowFacts(hwnd, appletMonitor, out var facts))
            {
                return;
            }

            var correction = SurroundingWindowLayoutPolicy.ComputeCorrection(appletBounds, workArea, appletDisplay, facts);
            if (correction is null)
                return;

            var bounds = correction.Bounds;
            _selfInducedMoves[hwnd] = DateTimeOffset.UtcNow;
            SetWindowPos(
                hwnd,
                IntPtr.Zero,
                (int)Math.Round(bounds.Left),
                (int)Math.Round(bounds.Top),
                (int)Math.Round(bounds.Width),
                (int)Math.Round(bounds.Height),
                SwpNoActivate | SwpNoOwnerZOrder | SwpNoZOrder);
        }
        catch (Exception exception)
        {
            _logger.Warn($"Ignoring inaccessible surrounding window {hwnd}: {exception.Message}");
        }
    }

    private bool TryGetAppletDisplay(
        out AppBounds appletBounds,
        out AppBounds workArea,
        out string displayDeviceName,
        out IntPtr monitor)
    {
        appletBounds = default!;
        workArea = default!;
        displayDeviceName = string.Empty;
        monitor = IntPtr.Zero;

        if (!GetWindowRect(_appletHwnd, out var appletRect))
            return false;

        monitor = MonitorFromWindow(_appletHwnd, MonitorDefaultToNearest);
        if (!TryGetMonitorInfo(monitor, out var monitorInfo))
            return false;

        appletBounds = appletRect.ToBounds();
        workArea = monitorInfo.rcWork.ToBounds();
        displayDeviceName = monitorInfo.szDevice;
        return true;
    }

    private bool TryGetWindowFacts(IntPtr hwnd, IntPtr appletMonitor, out SurroundingWindowFacts facts)
    {
        facts = default!;
        if (hwnd == _appletHwnd
            || !IsWindowVisible(hwnd)
            || IsIconic(hwnd)
            || IsZoomed(hwnd)
            || GetWindow(hwnd, GwOwner) != IntPtr.Zero
            || (GetWindowLong(hwnd, GwlExStyle) & WsExToolWindow) != 0
            || IsShellWindow(hwnd)
            || !GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero || monitor != appletMonitor || !TryGetMonitorInfo(monitor, out var monitorInfo))
            return false;

        facts = new SurroundingWindowFacts(
            rect.ToBounds(),
            monitorInfo.szDevice,
            IsVisibleApplicationWindow: true,
            IsMinimized: false,
            IsMaximized: false,
            IsFullscreen: rect.Equals(monitorInfo.rcMonitor));
        return true;
    }

    private static bool IsShellWindow(IntPtr hwnd)
    {
        var className = new StringBuilder(256);
        GetClassName(hwnd, className, className.Capacity);
        return className.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW";
    }

    private static bool TryGetMonitorInfo(IntPtr monitor, out MonitorInfoEx info)
    {
        info = new MonitorInfoEx { cbSize = Marshal.SizeOf<MonitorInfoEx>() };
        return monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info);
    }

    private const uint EventSystemMoveSizeStart = 0x000A;
    private const uint EventSystemMoveSizeEnd = 0x000B;
    private const uint EventSystemMinimizeEnd = 0x0017;
    private const uint EventObjectShow = 0x8002;
    private const uint EventObjectLocationChange = 0x800B;
    private const int ObjIdWindow = 0;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const uint GwOwner = 4;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;

    private delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr hwnd, int objectId, int childId, uint eventThread, uint eventTime);
    private delegate bool EnumWindowsDelegate(IntPtr hwnd, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventDelegate callback, uint processId, uint threadId, uint flags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsDelegate callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maximumCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public int dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public AppBounds ToBounds() => new(Left, Top, Right - Left, Bottom - Top);
    }
}
