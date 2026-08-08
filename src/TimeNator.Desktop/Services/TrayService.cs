using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace TimeNator.Desktop.Services;

/// <summary>
/// Keeps TimeNator alive in the notification area. Closing the window hides it, so a
/// running timer keeps running; only Quit from the tray menu ends the process.
/// </summary>
public class TrayService
{
    private bool _quitting;

    public void Install(Application app, IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var window = desktop.MainWindow!;

        window.Closing += (_, e) =>
        {
            if (_quitting)
                return;
            e.Cancel = true;
            window.Hide();
        };

        var open = new NativeMenuItem("Open TimeNator");
        open.Click += (_, _) => Show(window);
        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) =>
        {
            _quitting = true;
            desktop.Shutdown();
        };

        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://TimeNator.Desktop/Assets/timenator.ico"))),
            ToolTipText = "TimeNator",
            Menu = [open, new NativeMenuItemSeparator(), quit]
        };
        tray.Clicked += (_, _) => Show(window);
        TrayIcon.SetIcons(app, [tray]);
    }

    private static void Show(Window window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
