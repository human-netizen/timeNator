using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;

namespace TimeNator.Desktop.ViewModels;

public partial class MainWindowViewModel(IApiClient api) : ViewModelBase
{
    [ObservableProperty] public partial string HealthText { get; set; } = "Checking API...";

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var health = await api.GetHealthAsync();
            HealthText = $"API {health.Status}, database {health.Checks.GetValueOrDefault("database", "unknown")}";
        }
        catch (HttpRequestException ex)
        {
            HealthText = $"API unreachable: {ex.Message}";
        }
    }
}
