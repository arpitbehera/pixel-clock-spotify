using System.IO;
using System.Text.Json;

namespace TerminalClockSpotify.State;

public sealed record PersistedAppState(string DisplayDeviceName, string DockPosition);

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public AppStateStore(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);
        _path = Path.Combine(rootDirectory, "state.json");
    }

    public PersistedAppState? Load()
    {
        if (!File.Exists(_path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PersistedAppState>(File.ReadAllText(_path), Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(PersistedAppState state)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(state, Options));
    }
}
