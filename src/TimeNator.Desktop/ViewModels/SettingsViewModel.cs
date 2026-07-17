using CommunityToolkit.Mvvm.ComponentModel;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStore _store;

    public SettingsViewModel(SettingsStore store)
    {
        _store = store;
        var current = store.Current;
        IdleThresholdMinutes = current.IdleThresholdMinutes;
        PomodoroFocusMinutes = current.PomodoroFocusMinutes;
        PomodoroBreakMinutes = current.PomodoroBreakMinutes;
    }

    [ObservableProperty] public partial decimal? IdleThresholdMinutes { get; set; }
    [ObservableProperty] public partial decimal? PomodoroFocusMinutes { get; set; }
    [ObservableProperty] public partial decimal? PomodoroBreakMinutes { get; set; }

    partial void OnIdleThresholdMinutesChanged(decimal? value)
    {
        if (value is >= 1 and <= 120)
            _store.Save(_store.Current with { IdleThresholdMinutes = (int)value.Value });
    }

    partial void OnPomodoroFocusMinutesChanged(decimal? value)
    {
        if (value is >= 1 and <= 180)
            _store.Save(_store.Current with { PomodoroFocusMinutes = (int)value.Value });
    }

    partial void OnPomodoroBreakMinutesChanged(decimal? value)
    {
        if (value is >= 1 and <= 60)
            _store.Save(_store.Current with { PomodoroBreakMinutes = (int)value.Value });
    }
}
