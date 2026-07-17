namespace TimeNator.Desktop.Models;

public enum PomodoroPhase
{
    Focus,
    Break
}

/// <summary>
/// Alternates focus and break blocks. Focus is measured in studied (running) time, so
/// a manual or idle pause stretches the block rather than eating into it. Breaks are
/// measured in wall-clock time, because the session is paused for their whole length.
/// </summary>
public class PomodoroCycle(TimeSpan focus, TimeSpan breakLength)
{
    private TimeSpan _focusStartedAtElapsed;
    private DateTimeOffset _breakStartedAt;

    public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Focus;
    public int CompletedFocusBlocks { get; private set; }

    /// <summary>Advances the phase if the current block is over; returns the new phase when it changes.</summary>
    public PomodoroPhase? Update(TimeSpan elapsed, DateTimeOffset now)
    {
        if (Phase == PomodoroPhase.Focus && elapsed - _focusStartedAtElapsed >= focus)
        {
            Phase = PomodoroPhase.Break;
            _breakStartedAt = now;
            CompletedFocusBlocks++;
            return Phase;
        }

        if (Phase == PomodoroPhase.Break && now - _breakStartedAt >= breakLength)
        {
            StartFocus(elapsed);
            return Phase;
        }

        return null;
    }

    /// <summary>Ends a break early, when the user resumes by hand.</summary>
    public void StartFocus(TimeSpan elapsed)
    {
        Phase = PomodoroPhase.Focus;
        _focusStartedAtElapsed = elapsed;
    }

    public TimeSpan Remaining(TimeSpan elapsed, DateTimeOffset now)
    {
        var remaining = Phase == PomodoroPhase.Focus
            ? focus - (elapsed - _focusStartedAtElapsed)
            : breakLength - (now - _breakStartedAt);
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }
}
