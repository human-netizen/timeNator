using TimeNator.Desktop.Interop;
using TimeNator.Desktop.Models;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

/// <summary>
/// Watches the foreground window while a session runs. What it does about a non-allowed
/// app depends on the strictness setting: record it, warn, or take focus back. Records
/// are buffered and sent in one batch when the session ends.
/// </summary>
public class FocusGuard(
    IForegroundWatcher watcher,
    IApiClient api,
    SettingsStore settings,
    IWindowService windows,
    INotificationService notifications,
    TimeProvider clock)
{
    private DistractionTracker? _tracker;
    private FocusStrictness _strictness;

    /// <summary>Raised with the app's name when a warning is due. On the UI thread.</summary>
    public event Action<string>? Warning;

    public bool IsActive => _tracker is not null;

    public async Task StartAsync()
    {
        _strictness = settings.Current.FocusStrictness;
        if (_strictness == FocusStrictness.Off || IsActive)
            return;

        List<string> allowed;
        try
        {
            allowed = (await api.GetAllowedAppsAsync()).Select(a => a.ProcessName).ToList();
        }
        catch (ApiException)
        {
            allowed = []; // Offline: everything counts, which errs on the side of honesty.
        }

        _tracker = new DistractionTracker(allowed);
        watcher.ForegroundChanged += OnForegroundChanged;
        watcher.Start();
    }

    public void SetPaused(bool paused) => _tracker?.SetPaused(paused, clock.GetUtcNow());

    /// <summary>Stops watching and uploads what was recorded, linked to the saved session if there is one.</summary>
    public async Task StopAsync(Guid? sessionId)
    {
        if (_tracker is not { } tracker)
            return;
        _tracker = null;
        watcher.Stop();
        watcher.ForegroundChanged -= OnForegroundChanged;

        var records = tracker.Finish(clock.GetUtcNow());
        if (records.Count == 0)
            return;
        try
        {
            await api.AddDistractionEventsAsync(records
                .Select(r => new DistractionEventRequest(sessionId, r.ProcessName, r.WindowTitle, r.StartedAt,
                    r.DurationSeconds))
                .ToList());
        }
        catch (ApiException)
        {
            // Distraction logs are nice to have; losing one batch to a network error is acceptable.
        }
    }

    private void OnForegroundChanged(ForegroundApp app)
    {
        if (_tracker?.OnForeground(app.ProcessName, app.WindowTitle, clock.GetUtcNow()) != true)
            return;

        if (_strictness >= FocusStrictness.Warn)
        {
            Warning?.Invoke(app.ProcessName);
            notifications.Show(NotificationKind.Focus, "Stay focused", $"{app.ProcessName} is not on your allowed list.");
        }
        if (_strictness == FocusStrictness.ForceFocus)
            windows.BringMainToFront();
    }
}
