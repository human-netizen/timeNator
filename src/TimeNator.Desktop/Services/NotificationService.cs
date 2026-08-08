using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using TimeNator.Desktop.Views;

namespace TimeNator.Desktop.Services;

public enum NotificationKind
{
    Pomodoro,
    Idle,
    Focus,
    Groups
}

public record Toast(string Title, string Body);

public interface INotificationService
{
    void Show(NotificationKind kind, string title, string body);
}

/// <summary>
/// Small pop-ups in the bottom-right corner that do not steal focus, gated per kind by
/// the user's settings. One at a time: a new one replaces whatever is showing.
/// </summary>
public class NotificationService(SettingsStore settings) : INotificationService
{
    private static readonly TimeSpan ShowFor = TimeSpan.FromSeconds(5);
    private ToastWindow? _current;
    private DispatcherTimer? _closeTimer;

    public void Show(NotificationKind kind, string title, string body)
    {
        if (!IsEnabled(kind))
            return;
        Dispatcher.UIThread.Post(() => Display(new Toast(title, body)));
    }

    private bool IsEnabled(NotificationKind kind)
    {
        var s = settings.Current;
        return kind switch
        {
            NotificationKind.Pomodoro => s.NotifyPomodoro,
            NotificationKind.Idle => s.NotifyIdle,
            NotificationKind.Focus => s.NotifyFocus,
            _ => s.NotifyGroups
        };
    }

    private void Display(Toast toast)
    {
        _closeTimer?.Stop();
        _current?.Close();

        var window = new ToastWindow { DataContext = toast, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show();
        _current = window;

        // The window's size is only known after its first layout pass, so place it then.
        Dispatcher.UIThread.Post(() =>
        {
            if (window.Screens.Primary is not { } screen)
                return;
            var area = screen.WorkingArea;
            var size = PixelSize.FromSize(window.Bounds.Size, screen.Scaling);
            window.Position = new PixelPoint(area.Right - size.Width - 16, area.Bottom - size.Height - 16);
        }, DispatcherPriority.Loaded);

        _closeTimer = new DispatcherTimer { Interval = ShowFor };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            window.Close();
            if (_current == window)
                _current = null;
        };
        _closeTimer.Start();
    }
}
