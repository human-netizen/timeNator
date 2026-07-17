namespace TimeNator.Desktop.Models;

public enum TimerState
{
    Idle,
    Running,
    Paused
}

public record CompletedTiming(
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int DurationSeconds,
    int PausedSeconds,
    int MaxStreakSeconds);

/// <summary>Everything needed to rebuild a started timer, for example after a crash.</summary>
public record TimerSnapshot(
    DateTimeOffset StartedAt,
    TimerState State,
    TimeSpan RunningBefore,
    TimeSpan PausedBefore,
    DateTimeOffset StateSince,
    TimeSpan StreakBefore = default,
    TimeSpan MaxStreak = default)
{
    /// <summary>Closes the session at <paramref name="endedAt"/>, crediting the current state up to then.</summary>
    public CompletedTiming FinishAt(DateTimeOffset endedAt)
    {
        var inState = endedAt > StateSince ? endedAt - StateSince : TimeSpan.Zero;
        var runningNow = State == TimerState.Running ? inState : TimeSpan.Zero;
        var duration = (int)Math.Round((RunningBefore + runningNow).TotalSeconds);
        var total = (int)Math.Round((endedAt - StartedAt).TotalSeconds);
        var streak = TimeSpan.FromTicks(Math.Max(MaxStreak.Ticks, (StreakBefore + runningNow).Ticks));
        return new CompletedTiming(StartedAt, endedAt, duration, Math.Max(0, total - duration),
            Math.Min(duration, (int)Math.Round(streak.TotalSeconds)));
    }
}

/// <summary>
/// The client-side clock for one session. Tracks running and paused time separately
/// from wall-clock timestamps, so the result satisfies end = start + duration + paused.
/// Also tracks the longest concentration streak: running time uninterrupted by any
/// pause longer than <see cref="StreakGrace"/>.
/// </summary>
public class StudyTimer(TimeProvider clock)
{
    public static readonly TimeSpan StreakGrace = TimeSpan.FromSeconds(15);

    private TimeSpan _runningBefore;
    private TimeSpan _pausedBefore;
    private TimeSpan _streakBefore;
    private TimeSpan _maxStreak;
    private DateTimeOffset _stateSince;

    public TimerState State { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }

    public TimeSpan Elapsed => _runningBefore + RunningNow;
    public TimeSpan Paused => _pausedBefore + (State == TimerState.Paused ? Since() : TimeSpan.Zero);
    public TimeSpan CurrentStreak => _streakBefore + RunningNow;
    public TimeSpan MaxStreak => CurrentStreak > _maxStreak ? CurrentStreak : _maxStreak;

    private TimeSpan RunningNow => State == TimerState.Running ? Since() : TimeSpan.Zero;

    public void Start()
    {
        if (State != TimerState.Idle)
            throw new InvalidOperationException("The timer is already started.");
        StartedAt = _stateSince = clock.GetUtcNow();
        _runningBefore = _pausedBefore = _streakBefore = _maxStreak = TimeSpan.Zero;
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
        _streakBefore += at - _stateSince;
        _stateSince = at;
        State = TimerState.Paused;
    }

    public void Resume()
    {
        if (State != TimerState.Paused)
            return;
        var pause = Since();
        _pausedBefore += pause;
        if (pause > StreakGrace)
        {
            if (_streakBefore > _maxStreak)
                _maxStreak = _streakBefore;
            _streakBefore = TimeSpan.Zero;
        }
        _stateSince = clock.GetUtcNow();
        State = TimerState.Running;
    }

    public CompletedTiming Stop()
    {
        var result = Snapshot().FinishAt(clock.GetUtcNow());
        State = TimerState.Idle;
        StartedAt = null;
        _runningBefore = _pausedBefore = _streakBefore = _maxStreak = TimeSpan.Zero;
        return result;
    }

    public TimerSnapshot Snapshot() =>
        State == TimerState.Idle
            ? throw new InvalidOperationException("The timer is not running.")
            : new TimerSnapshot(StartedAt!.Value, State, _runningBefore, _pausedBefore, _stateSince,
                _streakBefore, _maxStreak);

    private TimeSpan Since() => clock.GetUtcNow() - _stateSince;
}
