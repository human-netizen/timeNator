using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class ShellViewModel(IAuthService auth, INavigator navigator) : ViewModelBase
{
    public string DisplayName => auth.DisplayName ?? "";

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await auth.LogoutAsync();
        navigator.GoTo<LoginViewModel>();
    }
}
