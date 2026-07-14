namespace TimeNator.Desktop.Models;

public enum TimerState
{
    Idle,
    Running,
    Paused
}

public record CompletedTiming(DateTimeOffset StartedAt, DateTimeOffset EndedAt, int DurationSeconds, int PausedSeconds);

/// <summary>Everything needed to rebuild a started timer, for example after a crash.</summary>
public record TimerSnapshot(
    DateTimeOffset StartedAt,
    TimerState State,
    TimeSpan RunningBefore,
    TimeSpan PausedBefore,
    DateTimeOffset StateSince)
{
    /// <summary>Closes the session at <paramref name="endedAt"/>, crediting the current state up to then.</summary>
    public CompletedTiming FinishAt(DateTimeOffset endedAt)
    {
        var inState = endedAt > StateSince ? endedAt - StateSince : TimeSpan.Zero;
        var running = RunningBefore + (State == TimerState.Running ? inState : TimeSpan.Zero);
        var duration = (int)Math.Round(running.TotalSeconds);
        var total = (int)Math.Round((endedAt - StartedAt).TotalSeconds);
        return new CompletedTiming(StartedAt, endedAt, duration, Math.Max(0, total - duration));
    }
}

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

    /// <summary>
    /// Pauses the timer. <paramref name="since"/> backdates the pause, for idle detection
    /// that notices the user left some minutes after they actually did.
    /// </summary>
    public void Pause(DateTimeOffset? since = null)
    {
        if (State != TimerState.Running)
            return;
        var now = clock.GetUtcNow();
        var at = since is { } s ? (s < _stateSince ? _stateSince : s > now ? now : s) : now;
        _runningBefore += at - _stateSince;
        _stateSince = at;
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
        var result = Snapshot().FinishAt(clock.GetUtcNow());
        State = TimerState.Idle;
        StartedAt = null;
        _runningBefore = _pausedBefore = TimeSpan.Zero;
        return result;
    }

    public TimerSnapshot Snapshot() =>
        State == TimerState.Idle
            ? throw new InvalidOperationException("The timer is not running.")
            : new TimerSnapshot(StartedAt!.Value, State, _runningBefore, _pausedBefore, _stateSince);

    private TimeSpan Since() => clock.GetUtcNow() - _stateSince;
}
