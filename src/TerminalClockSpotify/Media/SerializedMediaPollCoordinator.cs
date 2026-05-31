namespace TerminalClockSpotify.Media;

public sealed class SerializedMediaPollCoordinator(Func<Task> poll)
{
    private readonly object _gate = new();
    private Task _current = Task.CompletedTask;
    private bool _isRunning;
    private bool _pending;

    public Task RequestAsync()
    {
        lock (_gate)
        {
            if (_isRunning)
            {
                _pending = true;
                return _current;
            }

            _isRunning = true;
            _current = RunAsync();
            return _current;
        }
    }

    private async Task RunAsync()
    {
        try
        {
            while (true)
            {
                await poll();

                lock (_gate)
                {
                    if (!_pending)
                    {
                        _isRunning = false;
                        return;
                    }

                    _pending = false;
                }
            }
        }
        catch
        {
            lock (_gate)
            {
                _pending = false;
                _isRunning = false;
            }

            throw;
        }
    }
}
