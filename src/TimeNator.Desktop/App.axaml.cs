using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TimeNator.Desktop.Services;
using TimeNator.Desktop.ViewModels;
using TimeNator.Desktop.Views;

namespace TimeNator.Desktop;

public partial class App : Application
{
    private static readonly Uri ApiBaseAddress = new("http://localhost:5000/");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<IApiClient, ApiClient>(http => http.BaseAddress = ApiBaseAddress);
        services.AddTransient<MainWindowViewModel>();
        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
            _ = viewModel.LoadCommand.ExecuteAsync(null);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
