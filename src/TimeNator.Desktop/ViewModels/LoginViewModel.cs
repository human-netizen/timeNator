using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class LoginViewModel(IAuthService auth, INavigator navigator) : ViewModelBase
{
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubmitText), nameof(ToggleText))]
    public partial bool IsRegistering { get; set; }

    [ObservableProperty] public partial string? Error { get; set; }

    public string SubmitText => IsRegistering ? "Create account" : "Log in";
    public string ToggleText => IsRegistering ? "I already have an account" : "Create an account";

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegistering = !IsRegistering;
        Error = null;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = "Enter your email and password.";
            return;
        }

        Error = IsRegistering
            ? await auth.RegisterAsync(Email.Trim(), Password, DisplayName.Trim())
            : await auth.LoginAsync(Email.Trim(), Password);

        if (Error is null)
            navigator.GoTo<ShellViewModel>();
    }
}
