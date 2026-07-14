using System.Runtime.InteropServices;

namespace TimeNator.Desktop.Interop;

public interface IIdleDetector
{
    /// <summary>Time since the last keyboard or mouse input anywhere in the session.</summary>
    TimeSpan GetIdleTime();
}

/// <summary>Wraps user32's GetLastInputInfo. Reports zero idle time off Windows.</summary>
public sealed class IdleDetector : IIdleDetector
{
    public TimeSpan GetIdleTime()
    {
        if (!OperatingSystem.IsWindows())
            return TimeSpan.Zero;

        var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // Both values are milliseconds since boot as 32-bit counters; unsigned
        // subtraction stays correct across the 49.7-day wraparound.
        var idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);
}
