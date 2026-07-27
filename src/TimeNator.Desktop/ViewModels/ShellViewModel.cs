using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class ShellViewModel(
    IAuthService auth,
    INavigator navigator,
    IStudyHubClient hub,
    TimerViewModel timer,
    BackgroundAudioViewModel audio,
    SubjectsViewModel subjects,
    HistoryViewModel history,
    GroupsViewModel groups,
    LeaderboardViewModel leaderboard,
    SettingsViewModel settings) : ViewModelBase
{
    public string DisplayName => auth.DisplayName ?? "";

    public TimerViewModel Timer { get; } = timer;
    public BackgroundAudioViewModel Audio { get; } = audio;
    public SubjectsViewModel Subjects { get; } = subjects;
    public HistoryViewModel History { get; } = history;
    public GroupsViewModel Groups { get; } = groups;
    public LeaderboardViewModel Leaderboard { get; } = leaderboard;
    public SettingsViewModel Settings { get; } = settings;

    private const int LeaderboardTab = 3;

    [ObservableProperty] public partial int SelectedTab { get; set; }

    partial void OnSelectedTabChanged(int value) =>
        _ = value == LeaderboardTab ? Leaderboard.OpenAsync() : Leaderboard.CloseAsync();

    public override async Task ActivateAsync()
    {
        await hub.ConnectAsync();
        await Timer.ActivateAsync();
        await Subjects.LoadCommand.ExecuteAsync(null);
        await History.LoadCommand.ExecuteAsync(null);
        await Groups.ActivateAsync();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        Audio.Selected = BackgroundAudioViewModel.Options[0];
        await hub.DisconnectAsync();
        await auth.LogoutAsync();
        navigator.GoTo<LoginViewModel>();
    }
}
