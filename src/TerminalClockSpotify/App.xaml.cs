using System.IO;
using System.Windows;
using TerminalClockSpotify.Config;
using TerminalClockSpotify.Logging;
using TerminalClockSpotify.Media;
using TerminalClockSpotify.State;
using TerminalClockSpotify.ViewModels;

namespace TerminalClockSpotify;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TerminalClockSpotify");
        var logRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TerminalClockSpotify",
            "logs");

        var configLoader = new ConfigLoader(configRoot);
        var config = configLoader.Load();
        var logger = new RollingFileLogger(logRoot);
        var stateStore = new AppStateStore(configRoot);
        var mediaService = new WindowsMediaSessionService();
        var viewModel = new MainViewModel(mediaService, config.SpotifySourceAppIdContains);

        var window = new MainWindow(config, configLoader, stateStore, logger, viewModel);
        window.Show();
    }
}
