using System.Runtime.InteropServices;

namespace TimeNator.Desktop.Interop;

public interface ILoopingSound
{
    void Play(string wavPath);
    void Stop();
}

/// <summary>
/// Loops a WAV file through winmm's PlaySound. One sound at a time, which is all
/// background audio needs; does nothing off Windows.
/// </summary>
public sealed class LoopingSound : ILoopingSound
{
    private const uint SndAsync = 0x0001;
    private const uint SndNoDefault = 0x0002;
    private const uint SndLoop = 0x0008;
    private const uint SndFilename = 0x00020000;

    public void Play(string wavPath)
    {
        if (OperatingSystem.IsWindows())
            PlaySound(wavPath, IntPtr.Zero, SndAsync | SndLoop | SndNoDefault | SndFilename);
    }

    public void Stop()
    {
        if (OperatingSystem.IsWindows())
            PlaySound(null, IntPtr.Zero, 0);
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);
}
