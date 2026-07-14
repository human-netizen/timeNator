using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class ShellViewModel(
    IAuthService auth,
    INavigator navigator,
    TimerViewModel timer,
    SubjectsViewModel subjects,
    HistoryViewModel history,
    SettingsViewModel settings) : ViewModelBase
{
    public string DisplayName => auth.DisplayName ?? "";

    public TimerViewModel Timer { get; } = timer;
    public SubjectsViewModel Subjects { get; } = subjects;
    public HistoryViewModel History { get; } = history;
    public SettingsViewModel Settings { get; } = settings;

    public override async Task ActivateAsync()
    {
        await Timer.ActivateAsync();
        await Subjects.LoadCommand.ExecuteAsync(null);
        await History.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await auth.LogoutAsync();
        navigator.GoTo<LoginViewModel>();
    }
}
