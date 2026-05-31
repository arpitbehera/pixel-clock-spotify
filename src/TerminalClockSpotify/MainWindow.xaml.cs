using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TerminalClockSpotify.Config;
using TerminalClockSpotify.Input;
using TerminalClockSpotify.Logging;
using TerminalClockSpotify.Media;
using TerminalClockSpotify.Placement;
using TerminalClockSpotify.Scheduling;
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
    private readonly SerializedMediaPollCoordinator _mediaPollCoordinator;
    private readonly Stopwatch _lightweightElapsed = new();
    private LightweightUpdateScheduler? _lightweightScheduler;
    private DispatcherTimer? _lightweightTimer;
    private DispatcherTimer? _mediaTimer;
    private SurroundingWindowLayoutService? _surroundingWindowLayoutService;
    private bool _isDragging;
    private bool _isAlbumArtPressed;
    private System.Windows.Point _dragOffset;
    private System.Windows.Point _albumArtPressPoint;

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
        _mediaPollCoordinator = new SerializedMediaPollCoordinator(RefreshMediaCoreAsync);

        InitializeComponent();

        DataContext = _viewModel;
        ApplyVisualSettings();

        SourceInitialized += (_, _) =>
        {
            ApplyWindowStyles();
            _surroundingWindowLayoutService = new SurroundingWindowLayoutService(new WindowInteropHelper(this).Handle, _logger);
            _surroundingWindowLayoutService.Update(_config.ClickThrough);
        };
        Loaded += (_, _) =>
        {
            ApplyInitialPlacement();
            StartTimers();
        };
        Closed += (_, _) =>
        {
            StopTimers();
            _surroundingWindowLayoutService?.Dispose();
            _shutdown.Cancel();
        };
        ProgressTrack.SizeChanged += (_, _) => _viewModel.SetProgressTrackWidth(ProgressTrack.ActualWidth);

        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        MouseMove += MainWindow_MouseMove;
        MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
    }

    private void StartTimers()
    {
        StopTimers();
        _viewModel.RefreshClock(DateTimeOffset.Now);
        _viewModel.RefreshProgress();
        _viewModel.SetProgressTrackWidth(ProgressTrack.ActualWidth);
        _ = _mediaPollCoordinator.RequestAsync();

        _lightweightScheduler = new LightweightUpdateScheduler(_config.ClockUpdateIntervalMs, _config.ProgressUpdateIntervalMs);
        _lightweightElapsed.Restart();
        _lightweightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_lightweightScheduler.TimerIntervalMs) };
        _lightweightTimer.Tick += (_, _) =>
        {
            var updates = _lightweightScheduler.Tick(_lightweightElapsed.ElapsedMilliseconds);
            if (updates.Clock)
                _viewModel.RefreshClock(DateTimeOffset.Now);
            if (updates.Progress)
                _viewModel.RefreshProgress();
        };
        _lightweightTimer.Start();

        _mediaTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_config.MediaUpdateIntervalMs) };
        _mediaTimer.Tick += (_, _) => _ = _mediaPollCoordinator.RequestAsync();
        _mediaTimer.Start();
    }

    private void StopTimers()
    {
        _lightweightTimer?.Stop();
        _mediaTimer?.Stop();
        _lightweightElapsed.Stop();
    }

    private async Task RefreshMediaCoreAsync()
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
        BeginDrag(e.GetPosition(this));
        e.Handled = true;
    }

    private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isAlbumArtPressed && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(this);
            var gesture = AlbumArtGestureClassifier.Classify(
                current.X - _albumArtPressPoint.X,
                current.Y - _albumArtPressPoint.Y,
                SystemParameters.MinimumHorizontalDragDistance,
                SystemParameters.MinimumVerticalDragDistance);
            if (gesture == AlbumArtGesture.Click)
                return;

            _isAlbumArtPressed = false;
            _isDragging = true;
        }

        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
            return;

        var screenPoint = PointToScreen(e.GetPosition(this));
        Left = screenPoint.X - _dragOffset.X;
        Top = screenPoint.Y - _dragOffset.Y;
    }

    private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isAlbumArtPressed)
        {
            _isAlbumArtPressed = false;
            ReleaseMouseCapture();
            if (AlbumArtFrame.IsMouseOver)
                _ = TogglePlayPauseAsync();
            e.Handled = true;
            return;
        }

        if (!_isDragging)
            return;

        CompleteDrag();
    }

    private void AlbumArtFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isAlbumArtPressed = true;
        _albumArtPressPoint = e.GetPosition(this);
        _dragOffset = _albumArtPressPoint;
        CaptureMouse();
        e.Handled = true;
    }

    private void BeginDrag(System.Windows.Point offset)
    {
        _isDragging = true;
        _dragOffset = offset;
        CaptureMouse();
    }

    private void CompleteDrag()
    {
        _isDragging = false;
        ReleaseMouseCapture();

        var displays = GetDisplays();
        if (displays.Count == 0)
            return;

        var snapped = PlacementService.SnapToNearest(displays, Left, Top, Width, Height);
        ApplyBounds(snapped.Bounds);
        _stateStore.Save(new PersistedAppState(snapped.DisplayDeviceName, snapped.DockPosition));
    }

    private async Task TogglePlayPauseAsync()
    {
        try
        {
            if (await _viewModel.TogglePlayPauseAsync(_shutdown.Token))
                await _mediaPollCoordinator.RequestAsync();
            else
                _logger.Warn("Spotify play/pause toggle was unavailable.");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to toggle Spotify play/pause", exception);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _config = _configLoader.Load();
        ApplyVisualSettings();
        ApplyWindowStyles();
        ApplyInitialPlacement();
        StartTimers();
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
        _surroundingWindowLayoutService?.Update(_config.ClickThrough);
    }

    private void ApplyWindowStyles()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(hwnd, GwlExStyle);
        extendedStyle |= WsExToolWindow | WsExNoActivate;

        if (_config.ClickThrough)
            extendedStyle |= WsExTransparent;
        else
            extendedStyle &= ~WsExTransparent;

        SetWindowLong(hwnd, GwlExStyle, extendedStyle);
        _surroundingWindowLayoutService?.Update(_config.ClickThrough);
    }

    private void ApplyVisualSettings()
    {
        Topmost = _config.AlwaysOnTop;
        Opacity = _config.Opacity;
        Width = _config.FixedWindowWidth ?? 1200;
        Height = _config.FixedWindowHeight ?? 260;
        AlwaysOnTopMenuItem.IsChecked = Topmost;

        SetBrush("BackgroundBrush", _config.Palette.Background);
        SetBrush("PrimaryGreenBrush", _config.Palette.PrimaryGreen);
        SetBrush("DimGreenBrush", _config.Palette.DimGreen);
        SetBrush("SecondaryTextBrush", _config.Palette.SecondaryText);
        SetBrush("ProgressTrackBrush", _config.Palette.ProgressTrack);
    }

    private void SetBrush(string key, string value)
    {
        try
        {
            Resources[key] = (Brush)new BrushConverter().ConvertFromString(value)!;
        }
        catch (Exception exception)
        {
            _logger.Warn($"Ignoring invalid palette color {value} for {key}: {exception.Message}");
        }
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
