using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record AvatarChoice(string Key, string Glyph);

/// <summary>Profile (stored on the server) and machine preferences (stored locally).</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStore _store;
    private readonly IApiClient _api;
    private readonly ThemeService _themes;
    private readonly LaunchAtLogin _launchAtLogin;

    public SettingsViewModel(SettingsStore store, IApiClient api, ThemeService themes, LaunchAtLogin launchAtLogin)
    {
        _store = store;
        _api = api;
        _themes = themes;
        _launchAtLogin = launchAtLogin;
        var current = store.Current;
        IdleThresholdMinutes = current.IdleThresholdMinutes;
        PomodoroFocusMinutes = current.PomodoroFocusMinutes;
        PomodoroBreakMinutes = current.PomodoroBreakMinutes;
        NotifyPomodoro = current.NotifyPomodoro;
        NotifyIdle = current.NotifyIdle;
        NotifyFocus = current.NotifyFocus;
        NotifyGroups = current.NotifyGroups;
        StartWithWindows = launchAtLogin.IsEnabled;
    }

    public static IReadOnlyList<AvatarChoice> AvatarChoices { get; } =
        Avatars.Glyphs.Select(a => new AvatarChoice(a.Key, a.Value)).ToList();

    public static IReadOnlyList<string> ThemeChoices => Themes.All;

    /// <summary>Raised after the profile is saved, so the shell can refresh the name it shows.</summary>
    public event Action<ProfileResponse>? ProfileSaved;

    [ObservableProperty] public partial string DisplayName { get; set; } = "";
    [ObservableProperty] public partial string StatusMessage { get; set; } = "";
    [ObservableProperty] public partial AvatarChoice? Avatar { get; set; }
    [ObservableProperty] public partial string Theme { get; set; } = Themes.System;
    [ObservableProperty] public partial string? ProfileMessage { get; private set; }

    [ObservableProperty] public partial decimal? IdleThresholdMinutes { get; set; }
    [ObservableProperty] public partial decimal? PomodoroFocusMinutes { get; set; }
    [ObservableProperty] public partial decimal? PomodoroBreakMinutes { get; set; }

    [ObservableProperty] public partial bool NotifyPomodoro { get; set; }
    [ObservableProperty] public partial bool NotifyIdle { get; set; }
    [ObservableProperty] public partial bool NotifyFocus { get; set; }
    [ObservableProperty] public partial bool NotifyGroups { get; set; }

    [ObservableProperty] public partial bool StartWithWindows { get; set; }

    partial void OnNotifyPomodoroChanged(bool value) => _store.Save(_store.Current with { NotifyPomodoro = value });
    partial void OnNotifyIdleChanged(bool value) => _store.Save(_store.Current with { NotifyIdle = value });
    partial void OnNotifyFocusChanged(bool value) => _store.Save(_store.Current with { NotifyFocus = value });
    partial void OnNotifyGroupsChanged(bool value) => _store.Save(_store.Current with { NotifyGroups = value });
    partial void OnStartWithWindowsChanged(bool value) => _launchAtLogin.SetEnabled(value);

    public override async Task ActivateAsync()
    {
        try
        {
            var profile = await _api.GetProfileAsync();
            DisplayName = profile.DisplayName;
            StatusMessage = profile.StatusMessage ?? "";
            Avatar = AvatarChoices.FirstOrDefault(a => a.Key == profile.AvatarKey);
            Theme = profile.ThemeKey ?? Themes.System;
            _themes.Apply(Theme);
            ProfileSaved?.Invoke(profile);
        }
        catch (ApiException ex)
        {
            ProfileMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        try
        {
            var saved = await _api.UpdateProfileAsync(
                new UpdateProfileRequest(DisplayName.Trim(), StatusMessage, Avatar?.Key, Theme));
            ProfileMessage = "Saved.";
            ProfileSaved?.Invoke(saved);
        }
        catch (ApiException ex)
        {
            ProfileMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void PickAvatar(AvatarChoice choice) => Avatar = choice;

    partial void OnThemeChanged(string value) => _themes.Apply(value);

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
