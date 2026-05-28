using System.IO;

namespace TerminalClockSpotify.Logging;

public sealed class RollingFileLogger
{
    private readonly string _directory;
    private readonly int _maxBytes;
    private readonly int _retainedFiles;

    public RollingFileLogger(string logDirectory, int maxBytes = 262144, int retainedFiles = 3)
    {
        _directory = logDirectory;
        _maxBytes = maxBytes;
        _retainedFiles = retainedFiles;
        Directory.CreateDirectory(_directory);
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception exception) => Write("ERROR", $"{message}: {exception}");

    private string CurrentPath => Path.Combine(_directory, "app.log");

    private void Write(string level, string message)
    {
        RollIfNeeded();
        File.AppendAllText(CurrentPath, $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
    }

    private void RollIfNeeded()
    {
        if (!File.Exists(CurrentPath) || new FileInfo(CurrentPath).Length < _maxBytes)
            return;

        for (var i = _retainedFiles - 1; i >= 1; i--)
        {
            var source = Path.Combine(_directory, i == 1 ? "app.log" : $"app.{i - 1}.log");
            var target = Path.Combine(_directory, $"app.{i}.log");
            if (File.Exists(target))
                File.Delete(target);
            if (File.Exists(source))
                File.Move(source, target);
        }
    }
}
