using TimeNator.Desktop.Models;

namespace TimeNator.Desktop.Tests;

public class PomodoroCycleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);
    private readonly PomodoroCycle _cycle = new(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5));

    [Fact]
    public void Focus_ends_after_its_length_of_studied_time()
    {
        Assert.Null(_cycle.Update(TimeSpan.FromMinutes(24), T0.AddMinutes(24)));

        var changed = _cycle.Update(TimeSpan.FromMinutes(25), T0.AddMinutes(25));

        Assert.Equal(PomodoroPhase.Break, changed);
        Assert.Equal(1, _cycle.CompletedFocusBlocks);
    }

    [Fact]
    public void Paused_time_does_not_count_towards_focus()
    {
        // Thirty wall-clock minutes but only twenty studied: still focusing.
        Assert.Null(_cycle.Update(TimeSpan.FromMinutes(20), T0.AddMinutes(30)));
        Assert.Equal(TimeSpan.FromMinutes(5), _cycle.Remaining(TimeSpan.FromMinutes(20), T0.AddMinutes(30)));
    }

    [Fact]
    public void Break_ends_after_its_wall_clock_length()
    {
        _cycle.Update(TimeSpan.FromMinutes(25), T0.AddMinutes(25));

        Assert.Null(_cycle.Update(TimeSpan.FromMinutes(25), T0.AddMinutes(29)));
        var changed = _cycle.Update(TimeSpan.FromMinutes(25), T0.AddMinutes(30));

        Assert.Equal(PomodoroPhase.Focus, changed);
        Assert.Equal(TimeSpan.FromMinutes(25), _cycle.Remaining(TimeSpan.FromMinutes(25), T0.AddMinutes(30)));
    }

    [Fact]
    public void Skipping_a_break_starts_a_fresh_focus_block()
    {
        _cycle.Update(TimeSpan.FromMinutes(25), T0.AddMinutes(25));

        _cycle.StartFocus(TimeSpan.FromMinutes(25));

        Assert.Equal(PomodoroPhase.Focus, _cycle.Phase);
        Assert.Null(_cycle.Update(TimeSpan.FromMinutes(49), T0.AddMinutes(50)));
        Assert.Equal(PomodoroPhase.Break, _cycle.Update(TimeSpan.FromMinutes(50), T0.AddMinutes(51)));
    }
}
