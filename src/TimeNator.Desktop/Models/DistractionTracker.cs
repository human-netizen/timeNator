namespace TimeNator.Desktop.Models;

public record Distraction(string ProcessName, string WindowTitle, DateTimeOffset StartedAt, int DurationSeconds);

/// <summary>
/// Turns a stream of "this app now has focus" into distraction records: one per stretch
/// spent in an app that is not allowed, closed when an allowed app (or TimeNator) takes
/// focus back, or when studying pauses.
/// </summary>
public class DistractionTracker(IEnumerable<string> allowedProcesses)
{
    // Parts of the Windows shell. Clicking the taskbar or alt-tabbing through them is not a distraction.
    private static readonly string[] ShellProcesses =
    [
        "explorer", "shellexperiencehost", "startmenuexperiencehost", "searchhost", "searchapp",
        "lockapp", "textinputhost", "timenator.desktop"
    ];

    private readonly HashSet<string> _allowed =
        allowedProcesses.Concat(ShellProcesses).Select(p => p.ToLowerInvariant()).ToHashSet();

    private readonly List<Distraction> _finished = [];
    private (string Process, string Title, DateTimeOffset Since)? _open;
    private bool _paused;

    public IReadOnlyList<Distraction> Finished => _finished;

    /// <summary>Returns true when the newly focused app counts as a distraction.</summary>
    public bool OnForeground(string processName, string windowTitle, DateTimeOffset at)
    {
        processName = processName.ToLowerInvariant();
        if (_paused)
            return false;
        if (_open is { } open && open.Process == processName)
            return !_allowed.Contains(processName);

        Close(at);
        if (_allowed.Contains(processName))
            return false;

        _open = (processName, windowTitle, at);
        return true;
    }

    /// <summary>Paused time is not study time, so nothing done during it is a distraction.</summary>
    public void SetPaused(bool paused, DateTimeOffset at)
    {
        if (paused)
            Close(at);
        _paused = paused;
    }

    public IReadOnlyList<Distraction> Finish(DateTimeOffset at)
    {
        Close(at);
        return _finished;
    }

    private void Close(DateTimeOffset at)
    {
        if (_open is not { } open)
            return;
        var seconds = (int)Math.Round((at - open.Since).TotalSeconds);
        if (seconds > 0)
            _finished.Add(new Distraction(open.Process, open.Title, open.Since, seconds));
        _open = null;
    }
}
