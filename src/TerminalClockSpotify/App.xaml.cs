using System.IO;
using System.Windows;
using TerminalClockSpotify.Config;
using TerminalClockSpotify.Logging;
using TerminalClockSpotify.Media;
using TerminalClockSpotify.ViewModels;

namespace TerminalClockSpotify;

public partial class App : Application
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
        var mediaService = new WindowsMediaSessionService();
        var viewModel = new MainViewModel(mediaService, config.SpotifySourceAppIdContains);

        var window = new MainWindow(config, configLoader, logger, viewModel);
        window.Show();
    }
}
