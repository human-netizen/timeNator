using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class MainWindowViewModel(Navigator navigator, IAuthService auth) : ViewModelBase
{
    public Navigator Navigator { get; } = navigator;

    [ObservableProperty] public partial string StatusText { get; set; } = "Connecting...";

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (await auth.TryRestoreAsync())
            Navigator.GoTo<ShellViewModel>();
        else
            Navigator.GoTo<LoginViewModel>();
    }
}
