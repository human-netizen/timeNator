using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TimeNator.Desktop.Interop;

public record ForegroundApp(string ProcessName, string WindowTitle, int ProcessId);

public interface IForegroundWatcher : IDisposable
{
    /// <summary>Raised on the thread that started the watcher, whenever another window takes focus.</summary>
    event Action<ForegroundApp>? ForegroundChanged;

    void Start();
    void Stop();
}

/// <summary>
/// Reports every foreground window change through SetWinEventHook. The hook is out of
/// context, so Windows delivers callbacks through the message loop of the thread that
/// registered it; start it on the UI thread. Off Windows it never fires.
/// </summary>
public sealed class ForegroundWatcher : IForegroundWatcher
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;

    // Kept in a field: the native side holds only a function pointer, so without this
    // reference the GC could collect the delegate while the hook still calls it.
    private readonly WinEventProc _callback;
    private IntPtr _hook;

    public ForegroundWatcher() => _callback = OnWinEvent;

    public event Action<ForegroundApp>? ForegroundChanged;

    public void Start()
    {
        if (_hook != IntPtr.Zero || !OperatingSystem.IsWindows())
            return;
        _hook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero, _callback, 0, 0,
            WinEventOutOfContext | WinEventSkipOwnProcess);
    }

    public void Stop()
    {
        if (_hook == IntPtr.Zero)
            return;
        UnhookWinEvent(_hook);
        _hook = IntPtr.Zero;
    }

    public void Dispose() => Stop();

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread,
        uint time)
    {
        if (hwnd == IntPtr.Zero)
            return;
        GetWindowThreadProcessId(hwnd, out var pid);
        string processName;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            processName = process.ProcessName.ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return; // The process exited between the event and the lookup.
        }

        var title = new StringBuilder(256);
        GetWindowText(hwnd, title, title.Capacity);
        ForegroundChanged?.Invoke(new ForegroundApp(processName, title.ToString(), (int)pid));
    }

    private delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild,
        uint thread, uint time);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback,
        uint processId, uint threadId, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);
}
