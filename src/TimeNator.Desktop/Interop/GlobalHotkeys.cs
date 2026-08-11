using System.Runtime.InteropServices;

namespace TimeNator.Desktop.Interop;

[Flags]
public enum HotkeyModifiers : uint
{
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    NoRepeat = 0x4000
}

/// <summary>
/// System-wide keyboard shortcuts through RegisterHotKey. Windows posts WM_HOTKEY to the
/// window that registered, so the owner of that window's message loop must call
/// <see cref="TryHandle"/> for each message. Off Windows, registration quietly fails.
/// </summary>
public sealed class GlobalHotkeys(IntPtr hwnd) : IDisposable
{
    public const uint WmHotkey = 0x0312;

    private readonly Dictionary<int, Action> _actions = [];
    private int _nextId = 0x5000;

    /// <summary>Returns false when another application already owns the combination.</summary>
    public bool Register(HotkeyModifiers modifiers, uint virtualKey, Action action)
    {
        if (!OperatingSystem.IsWindows())
            return false;
        var id = _nextId++;
        if (!RegisterHotKey(hwnd, id, (uint)(modifiers | HotkeyModifiers.NoRepeat), virtualKey))
            return false;
        _actions[id] = action;
        return true;
    }

    public bool TryHandle(uint message, IntPtr wParam)
    {
        if (message != WmHotkey || !_actions.TryGetValue((int)wParam, out var action))
            return false;
        action();
        return true;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys)
            UnregisterHotKey(hwnd, id);
        _actions.Clear();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
