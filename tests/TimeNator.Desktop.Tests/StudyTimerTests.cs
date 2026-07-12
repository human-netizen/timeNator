using Microsoft.Extensions.Time.Testing;
using TimeNator.Desktop.Models;

namespace TimeNator.Desktop.Tests;

public class StudyTimerTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 7, 11, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Elapsed_counts_only_running_time()
    {
        var timer = new StudyTimer(_clock);

        timer.Start();
        _clock.Advance(TimeSpan.FromMinutes(10));
        timer.Pause();
        _clock.Advance(TimeSpan.FromMinutes(3));
        timer.Resume();
        _clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(15), timer.Elapsed);
        Assert.Equal(TimeSpan.FromMinutes(3), timer.Paused);
    }

    [Fact]
    public void Stop_returns_a_consistent_session()
    {
        var timer = new StudyTimer(_clock);
        var start = _clock.GetUtcNow();

        timer.Start();
        _clock.Advance(TimeSpan.FromSeconds(125.4));
        timer.Pause();
        _clock.Advance(TimeSpan.FromSeconds(30));
        timer.Resume();
        _clock.Advance(TimeSpan.FromSeconds(60));
        var result = timer.Stop();

        Assert.Equal(start, result.StartedAt);
        Assert.Equal(185, result.DurationSeconds);
        Assert.Equal(30, result.PausedSeconds);
        Assert.InRange(
            (result.EndedAt - result.StartedAt.AddSeconds(result.DurationSeconds + result.PausedSeconds)).Duration(),
            TimeSpan.Zero, TimeSpan.FromSeconds(1));
        Assert.Equal(TimerState.Idle, timer.State);
    }

    [Fact]
    public void Stopping_while_paused_keeps_the_trailing_pause()
    {
        var timer = new StudyTimer(_clock);

        timer.Start();
        _clock.Advance(TimeSpan.FromMinutes(1));
        timer.Pause();
        _clock.Advance(TimeSpan.FromMinutes(2));
        var result = timer.Stop();

        Assert.Equal(60, result.DurationSeconds);
        Assert.Equal(120, result.PausedSeconds);
    }

    [Fact]
    public void Snapshot_finishes_at_the_last_seen_time()
    {
        var timer = new StudyTimer(_clock);
        timer.Start();
        _clock.Advance(TimeSpan.FromMinutes(20));
        var snapshot = timer.Snapshot();
        var lastSeen = _clock.GetUtcNow().AddMinutes(5);

        var result = snapshot.FinishAt(lastSeen);

        Assert.Equal(25 * 60, result.DurationSeconds);
        Assert.Equal(0, result.PausedSeconds);
        Assert.Equal(lastSeen, result.EndedAt);
    }

    [Fact]
    public void Starting_twice_throws()
    {
        var timer = new StudyTimer(_clock);
        timer.Start();

        Assert.Throws<InvalidOperationException>(timer.Start);
    }
}
