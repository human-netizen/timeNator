using System.Text.Json;

namespace TimeNator.Desktop.Services;

public record AppSettings
{
    public int IdleThresholdMinutes { get; init; } = 5;
    public int PomodoroFocusMinutes { get; init; } = 25;
    public int PomodoroBreakMinutes { get; init; } = 5;
}

/// <summary>Per-machine preferences, kept as JSON next to the other local files.</summary>
public class SettingsStore
{
    private readonly string _path = Path.Combine(AppPaths.DataDirectory, "settings.json");
    private AppSettings? _current;

    public event Action<AppSettings>? Changed;

    public AppSettings Current => _current ??= Load();

    public void Save(AppSettings settings)
    {
        _current = settings;
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings));
        Changed?.Invoke(settings);
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Fall back to defaults rather than refusing to start.
        }
        return new AppSettings();
    }
}
