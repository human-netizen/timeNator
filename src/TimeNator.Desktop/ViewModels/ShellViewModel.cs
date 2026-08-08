using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;

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
    FocusViewModel focus,
    PlannerViewModel planner,
    TimetableViewModel timetable,
    SettingsViewModel settings) : ViewModelBase
{
    [ObservableProperty] public partial string DisplayName { get; private set; } = auth.DisplayName ?? "";
    [ObservableProperty] public partial string AvatarGlyph { get; private set; } = Avatars.GlyphFor(null);

    public TimerViewModel Timer { get; } = timer;
    public BackgroundAudioViewModel Audio { get; } = audio;
    public SubjectsViewModel Subjects { get; } = subjects;
    public HistoryViewModel History { get; } = history;
    public GroupsViewModel Groups { get; } = groups;
    public LeaderboardViewModel Leaderboard { get; } = leaderboard;
    public FocusViewModel Focus { get; } = focus;
    public PlannerViewModel Planner { get; } = planner;
    public TimetableViewModel Timetable { get; } = timetable;
    public SettingsViewModel Settings { get; } = settings;

    private const int LeaderboardTab = 5;

    [ObservableProperty] public partial int SelectedTab { get; set; }

    partial void OnSelectedTabChanged(int value) =>
        _ = value == LeaderboardTab ? Leaderboard.OpenAsync() : Leaderboard.CloseAsync();

    public override async Task ActivateAsync()
    {
        Settings.ProfileSaved += profile =>
        {
            DisplayName = profile.DisplayName;
            AvatarGlyph = Avatars.GlyphFor(profile.AvatarKey);
        };
        await Settings.ActivateAsync();
        await hub.ConnectAsync();
        await Timer.ActivateAsync();
        await Subjects.LoadCommand.ExecuteAsync(null);
        await History.LoadCommand.ExecuteAsync(null);
        await Groups.ActivateAsync();
        await Focus.ActivateAsync();
        await Planner.ActivateAsync();
        await Timetable.ActivateAsync();
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
