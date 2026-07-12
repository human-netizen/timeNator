using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class ShellViewModel(
    IAuthService auth,
    INavigator navigator,
    TimerViewModel timer,
    SubjectsViewModel subjects) : ViewModelBase
{
    public string DisplayName => auth.DisplayName ?? "";

    public TimerViewModel Timer { get; } = timer;
    public SubjectsViewModel Subjects { get; } = subjects;

    public override Task ActivateAsync() => Subjects.LoadCommand.ExecuteAsync(null);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await auth.LogoutAsync();
        navigator.GoTo<LoginViewModel>();
    }
}
