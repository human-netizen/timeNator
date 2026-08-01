using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record RunningApp(string ProcessName, string Title);

/// <summary>Which applications are fine to use during a session, and how strictly the rest are handled.</summary>
public partial class FocusViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly SettingsStore _settings;

    public FocusViewModel(IApiClient api, SettingsStore settings)
    {
        _api = api;
        _settings = settings;
        Strictness = settings.Current.FocusStrictness;
    }

    public static IReadOnlyList<FocusStrictness> Levels { get; } = Enum.GetValues<FocusStrictness>();

    public ObservableCollection<AllowedAppResponse> AllowedApps { get; } = [];
    public ObservableCollection<RunningApp> RunningApps { get; } = [];

    [ObservableProperty] public partial FocusStrictness Strictness { get; set; }
    [ObservableProperty] public partial string? Error { get; private set; }

    public string StrictnessHelp => Strictness switch
    {
        FocusStrictness.Off => "Nothing is watched.",
        FocusStrictness.LogOnly => "Switching to other apps is recorded silently.",
        FocusStrictness.Warn => "Switching to other apps is recorded and you get a warning.",
        _ => "Switching to other apps is recorded and TimeNator takes focus back."
    };

    partial void OnStrictnessChanged(FocusStrictness value)
    {
        _settings.Save(_settings.Current with { FocusStrictness = value });
        OnPropertyChanged(nameof(StrictnessHelp));
    }

    public override Task ActivateAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var allowed = await _api.GetAllowedAppsAsync();
            AllowedApps.Clear();
            foreach (var app in allowed)
                AllowedApps.Add(app);
            Error = null;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        LoadRunningApps();
    }

    [RelayCommand]
    private async Task AllowAsync(RunningApp app)
    {
        try
        {
            AllowedApps.Add(await _api.AddAllowedAppAsync(new AllowedAppRequest(app.ProcessName, app.Title)));
            RunningApps.Remove(app);
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(AllowedAppResponse app)
    {
        try
        {
            await _api.RemoveAllowedAppAsync(app.Id);
            AllowedApps.Remove(app);
            LoadRunningApps();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    /// <summary>Processes with a visible main window, which is what a user thinks of as "an app".</summary>
    private void LoadRunningApps()
    {
        var allowed = AllowedApps.Select(a => a.ProcessName).ToHashSet();
        var ownId = Environment.ProcessId;
        var running = Process.GetProcesses()
            .Where(p => p.Id != ownId && p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle.Length > 0)
            .Select(p => new RunningApp(p.ProcessName.ToLowerInvariant(), Shorten(Describe(p))))
            .Where(a => !allowed.Contains(a.ProcessName))
            .DistinctBy(a => a.ProcessName)
            .OrderBy(a => a.ProcessName)
            .ToList();

        RunningApps.Clear();
        foreach (var app in running)
            RunningApps.Add(app);
    }

    /// <summary>The product name from the executable, e.g. "Visual Studio Code"; the window title if unreadable.</summary>
    private static string Describe(Process process)
    {
        try
        {
            var description = process.MainModule?.FileVersionInfo.FileDescription;
            if (!string.IsNullOrWhiteSpace(description))
                return description;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Elevated or exited processes cannot be inspected.
        }
        return process.MainWindowTitle;
    }

    private static string Shorten(string title) => title.Length > 60 ? title[..57] + "..." : title;
}
