using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using TerminalClockSpotify.Config;
using TerminalClockSpotify.Logging;
using TerminalClockSpotify.Placement;
using TerminalClockSpotify.State;
using TerminalClockSpotify.ViewModels;

namespace TerminalClockSpotify;

public partial class MainWindow : Window
{
    private AppConfig _config;
    private readonly ConfigLoader _configLoader;
    private readonly AppStateStore _stateStore;
    private readonly RollingFileLogger _logger;
    private readonly MainViewModel _viewModel;
    private readonly CancellationTokenSource _shutdown = new();
    private DispatcherTimer? _clockTimer;
    private DispatcherTimer? _mediaTimer;
    private bool _isDragging;
    private System.Windows.Point _dragOffset;

    public MainWindow(
        AppConfig config,
        ConfigLoader configLoader,
        AppStateStore stateStore,
        RollingFileLogger logger,
        MainViewModel viewModel)
    {
        _config = config;
        _configLoader = configLoader;
        _stateStore = stateStore;
        _logger = logger;
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;
        Topmost = _config.AlwaysOnTop;
        Opacity = _config.Opacity;
        Width = _config.FixedWindowWidth ?? 1200;
        Height = _config.FixedWindowHeight ?? 260;
        AlwaysOnTopMenuItem.IsChecked = Topmost;

        SourceInitialized += (_, _) => ApplyWindowStyles();
        Loaded += (_, _) =>
        {
            ApplyInitialPlacement();
            StartTimers();
        };
        Closed += (_, _) => _shutdown.Cancel();
        ProgressTrack.SizeChanged += (_, _) => _viewModel.SetProgressTrackWidth(ProgressTrack.ActualWidth);

        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        MouseMove += MainWindow_MouseMove;
        MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
    }

    private void StartTimers()
    {
        _viewModel.RefreshClock(DateTimeOffset.Now);
        _viewModel.SetProgressTrackWidth(ProgressTrack.ActualWidth);
        _ = RefreshMediaAsync();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_config.ClockUpdateIntervalMs) };
        _clockTimer.Tick += (_, _) => _viewModel.RefreshClock(DateTimeOffset.Now);
        _clockTimer.Start();

        _mediaTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_config.MediaUpdateIntervalMs) };
        _mediaTimer.Tick += async (_, _) => await RefreshMediaAsync();
        _mediaTimer.Start();
    }

    private async Task RefreshMediaAsync()
    {
        try
        {
            await _viewModel.RefreshMediaAsync(_shutdown.Token);
            _viewModel.SetProgressTrackWidth(ProgressTrack.ActualWidth);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to refresh media state", exception);
        }
    }

    private void ApplyInitialPlacement()
    {
        var displays = GetDisplays();
        if (displays.Count == 0)
            return;

        var state = _stateStore.Load();
        var display = PlacementService.SelectTarget(displays, _config.TargetDisplayLabel, state?.DisplayDeviceName);
        var dockPosition = state?.DockPosition ?? _config.DockPosition;
        ApplyBounds(PlacementService.ComputeBounds(
            display,
            dockPosition,
            _config.WindowWidthRatio,
            _config.WindowHeightRatio,
            _config.FixedWindowWidth,
            _config.FixedWindowHeight));
    }

    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragOffset = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
            return;

        var screenPoint = PointToScreen(e.GetPosition(this));
        Left = screenPoint.X - _dragOffset.X;
        Top = screenPoint.Y - _dragOffset.Y;
    }

    private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        ReleaseMouseCapture();

        var displays = GetDisplays();
        if (displays.Count == 0)
            return;

        var snapped = PlacementService.SnapToNearest(displays, Left, Top, Width, Height);
        ApplyBounds(snapped.Bounds);
        _stateStore.Save(new PersistedAppState(snapped.DisplayDeviceName, snapped.DockPosition));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _config = _configLoader.Load();
        Topmost = _config.AlwaysOnTop;
        AlwaysOnTopMenuItem.IsChecked = Topmost;
        Opacity = _config.Opacity;
        ApplyInitialPlacement();
        await RefreshMediaAsync();
    }

    private void Config_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(_configLoader.ConfigPath) { UseShellExecute = true });
    }

    private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopMenuItem.IsChecked;
        _config = _config with { AlwaysOnTop = Topmost };
        _configLoader.Save(_config);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static IReadOnlyList<DisplayInfo> GetDisplays()
    {
        var displays = new List<DisplayInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            var info = new MonitorInfoEx();
            info.cbSize = Marshal.SizeOf<MonitorInfoEx>();

            if (GetMonitorInfo(monitor, ref info))
            {
                displays.Add(new DisplayInfo(
                    (displays.Count + 1).ToString(),
                    info.szDevice,
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top,
                    1.0,
                    (info.dwFlags & MonitorInfofPrimary) != 0));
            }

            return true;
        }, IntPtr.Zero);

        return displays;
    }

    private void ApplyBounds(AppBounds bounds)
    {
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
    }

    private void ApplyWindowStyles()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(hwnd, GwlExStyle);
        extendedStyle |= WsExToolWindow | WsExNoActivate;

        if (_config.ClickThrough)
            extendedStyle |= WsExTransparent;

        SetWindowLong(hwnd, GwlExStyle, extendedStyle);
    }

    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExTransparent = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    private const int MonitorInfofPrimary = 1;

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
    }
}
