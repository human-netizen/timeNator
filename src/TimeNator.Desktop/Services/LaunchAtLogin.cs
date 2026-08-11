using Microsoft.Win32;

namespace TimeNator.Desktop.Services;

/// <summary>
/// Starts TimeNator when the user signs in, through the per-user Run key. The registry
/// value itself is the setting, so turning it off in Task Manager is reflected here too.
/// </summary>
public class LaunchAtLogin
{
    /// <summary>Passed on the Run key command line so the app starts in the tray.</summary>
    public const string MinimizedArgument = "--minimized";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TimeNator";

    public bool IsEnabled
    {
        get
        {
            if (!OperatingSystem.IsWindows())
                return false;
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string command && command.Contains(Environment.ProcessPath ?? "?");
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows() || Environment.ProcessPath is not { } exe)
            return;
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
            key.SetValue(ValueName, $"\"{exe}\" {MinimizedArgument}");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
