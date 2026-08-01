using System.Runtime.InteropServices;

namespace TimeNator.Desktop.Interop;

/// <summary>
/// Pulls a window to the front. Windows refuses SetForegroundWindow from a process that
/// does not own the foreground, so this briefly attaches to the foreground thread's input
/// queue, which is the documented way to be allowed to take focus.
/// </summary>
public static class WindowActivator
{
    private const int SwRestore = 9;

    public static void BringToFront(IntPtr hwnd)
    {
        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero)
            return;

        var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var ownThread = GetCurrentThreadId();
        var attached = foregroundThread != ownThread && AttachThreadInput(ownThread, foregroundThread, true);
        try
        {
            if (IsIconic(hwnd))
                ShowWindow(hwnd, SwRestore);
            SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
                AttachThreadInput(ownThread, foregroundThread, false);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool attach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);
}
