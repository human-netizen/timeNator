namespace TimeNator.Desktop.Models;

public enum TimerState
{
    Idle,
    Running,
    Paused
}

public record CompletedTiming(DateTimeOffset StartedAt, DateTimeOffset EndedAt, int DurationSeconds, int PausedSeconds);

/// <summary>
/// The client-side clock for one session. Tracks running and paused time separately
/// from wall-clock timestamps, so the result satisfies end = start + duration + paused.
/// </summary>
public class StudyTimer(TimeProvider clock)
{
    private TimeSpan _runningBefore;
    private TimeSpan _pausedBefore;
    private DateTimeOffset _stateSince;

    public TimerState State { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }

    public TimeSpan Elapsed => _runningBefore + (State == TimerState.Running ? Since() : TimeSpan.Zero);
    public TimeSpan Paused => _pausedBefore + (State == TimerState.Paused ? Since() : TimeSpan.Zero);

    public void Start()
    {
        if (State != TimerState.Idle)
            throw new InvalidOperationException("The timer is already started.");
        StartedAt = _stateSince = clock.GetUtcNow();
        _runningBefore = _pausedBefore = TimeSpan.Zero;
        State = TimerState.Running;
    }

    public void Pause()
    {
        if (State != TimerState.Running)
            return;
        _runningBefore += Since();
        _stateSince = clock.GetUtcNow();
        State = TimerState.Paused;
    }

    public void Resume()
    {
        if (State != TimerState.Paused)
            return;
        _pausedBefore += Since();
        _stateSince = clock.GetUtcNow();
        State = TimerState.Running;
    }

    public CompletedTiming Stop()
    {
        if (State == TimerState.Idle)
            throw new InvalidOperationException("The timer is not running.");

        var endedAt = clock.GetUtcNow();
        var duration = (int)Math.Round(Elapsed.TotalSeconds);
        var paused = (int)Math.Round((endedAt - StartedAt!.Value).TotalSeconds) - duration;
        var result = new CompletedTiming(StartedAt.Value, endedAt, duration, Math.Max(0, paused));

        State = TimerState.Idle;
        StartedAt = null;
        _runningBefore = _pausedBefore = TimeSpan.Zero;
        return result;
    }

    private TimeSpan Since() => clock.GetUtcNow() - _stateSince;
}
