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
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TokenStore>();

        services.AddHttpClient("auth", http => http.BaseAddress = ApiBaseAddress);
        services.AddSingleton<IAuthService>(sp => new AuthService(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("auth"),
            sp.GetRequiredService<TokenStore>(),
            sp.GetRequiredService<TimeProvider>()));

        services.AddTransient<AuthHeaderHandler>();
        services.AddHttpClient<IApiClient, ApiClient>(http => http.BaseAddress = ApiBaseAddress)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddSingleton<Navigator>();
        services.AddSingleton<INavigator>(sp => sp.GetRequiredService<Navigator>());
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
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
