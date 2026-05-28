using System.Windows;
using System.Windows.Threading;
using TerminalClockSpotify.Config;
using TerminalClockSpotify.Logging;
using TerminalClockSpotify.ViewModels;

namespace TerminalClockSpotify;

public partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly ConfigLoader _configLoader;
    private readonly RollingFileLogger _logger;
    private readonly MainViewModel _viewModel;
    private readonly CancellationTokenSource _shutdown = new();
    private DispatcherTimer? _clockTimer;
    private DispatcherTimer? _mediaTimer;

    public MainWindow(AppConfig config, ConfigLoader configLoader, RollingFileLogger logger, MainViewModel viewModel)
    {
        _config = config;
        _configLoader = configLoader;
        _logger = logger;
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;
        Topmost = _config.AlwaysOnTop;
        Opacity = _config.Opacity;
        Width = _config.FixedWindowWidth ?? 1200;
        Height = _config.FixedWindowHeight ?? 260;

        Loaded += (_, _) => StartTimers();
        Closed += (_, _) => _shutdown.Cancel();
        ProgressTrack.SizeChanged += (_, _) => _viewModel.SetProgressTrackWidth(ProgressTrack.ActualWidth);
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
}
