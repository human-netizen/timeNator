using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using TimeNator.Desktop.Interop;
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
        services.AddTransient<SubjectsViewModel>();
        services.AddTransient<TimerViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<ManualEntryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<IIdleDetector, IdleDetector>();
        services.AddSingleton<SubjectCatalog>();
        services.AddSingleton<SessionJournal>();
        services.AddSingleton<SessionUploader>();
        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
            // Posted so it runs inside the UI loop, where awaits resume on the UI thread.
            Dispatcher.UIThread.Post(() => viewModel.LoadCommand.Execute(null));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
