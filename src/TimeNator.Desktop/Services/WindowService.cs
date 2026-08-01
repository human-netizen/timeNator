using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using TimeNator.Desktop.Interop;
using TimeNator.Desktop.ViewModels;
using TimeNator.Desktop.Views;

namespace TimeNator.Desktop.Services;

public interface IWindowService
{
    void ShowDeskMode(TimerViewModel timer);
    void BringMainToFront();
}

/// <summary>Opens and focuses windows so view models never touch window types.</summary>
public class WindowService : IWindowService
{
    public void ShowDeskMode(TimerViewModel timer)
    {
        var viewModel = new DeskModeViewModel(timer);
        var window = new DeskModeWindow { DataContext = viewModel };
        viewModel.CloseRequested += window.Close;
        window.Closed += (_, _) => viewModel.Dispose();
        window.Show();
    }

    public void BringMainToFront()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } main })
            WindowActivator.BringToFront(main.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
    }
}
