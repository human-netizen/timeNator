using Avalonia.Controls;
using TimeNator.Desktop.Interop;

namespace TimeNator.Desktop.Services;

/// <summary>
/// Ctrl+Alt+S starts or stops the timer and Ctrl+Alt+P pauses or resumes it, from any
/// application. The shortcuts are registered on the main window and reach the timer as events.
/// </summary>
public class HotkeyService
{
    private const uint KeyS = 0x53;
    private const uint KeyP = 0x50;

    private GlobalHotkeys? _hotkeys;

    public event Action? StartStopPressed;
    public event Action? PauseResumePressed;

    /// <summary>Which shortcuts could not be registered because another app holds them.</summary>
    public IReadOnlyList<string> Unavailable { get; private set; } = [];

    public void Install(Window window)
    {
        window.Opened += (_, _) =>
        {
            if (!OperatingSystem.IsWindows() || window.TryGetPlatformHandle()?.Handle is not { } hwnd)
                return;

            _hotkeys = new GlobalHotkeys(hwnd);
            var unavailable = new List<string>();
            if (!_hotkeys.Register(HotkeyModifiers.Control | HotkeyModifiers.Alt, KeyS,
                    () => StartStopPressed?.Invoke()))
                unavailable.Add("Ctrl+Alt+S");
            if (!_hotkeys.Register(HotkeyModifiers.Control | HotkeyModifiers.Alt, KeyP,
                    () => PauseResumePressed?.Invoke()))
                unavailable.Add("Ctrl+Alt+P");
            Unavailable = unavailable;

            Win32Properties.AddWndProcHookCallback(window, WndProc);
        };
        window.Closed += (_, _) => _hotkeys?.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeys?.TryHandle(message, wParam) == true)
            handled = true;
        return IntPtr.Zero;
    }
}
