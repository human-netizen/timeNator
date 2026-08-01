using TimeNator.Desktop.Models;

namespace TimeNator.Desktop.Tests;

public class DistractionTrackerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Time_in_a_blocked_app_is_recorded_until_an_allowed_app_returns()
    {
        var tracker = new DistractionTracker(["code"]);

        Assert.True(tracker.OnForeground("MSEdge", "News", T0));
        Assert.False(tracker.OnForeground("code", "Editor", T0.AddSeconds(90)));

        var d = Assert.Single(tracker.Finish(T0.AddMinutes(10)));
        Assert.Equal("msedge", d.ProcessName);
        Assert.Equal(90, d.DurationSeconds);
    }

    [Fact]
    public void Switching_between_blocked_apps_makes_separate_records()
    {
        var tracker = new DistractionTracker([]);

        tracker.OnForeground("msedge", "a", T0);
        tracker.OnForeground("discord", "b", T0.AddSeconds(30));
        var records = tracker.Finish(T0.AddSeconds(50));

        Assert.Equal([("msedge", 30), ("discord", 20)], records.Select(r => (r.ProcessName, r.DurationSeconds)));
    }

    [Fact]
    public void Shell_and_own_windows_are_never_distractions()
    {
        var tracker = new DistractionTracker([]);

        Assert.False(tracker.OnForeground("explorer", "Taskbar", T0));
        Assert.False(tracker.OnForeground("TimeNator.Desktop", "TimeNator", T0.AddSeconds(5)));
        Assert.Empty(tracker.Finish(T0.AddSeconds(10)));
    }

    [Fact]
    public void Pausing_closes_the_open_distraction_and_ignores_later_switches()
    {
        var tracker = new DistractionTracker([]);

        tracker.OnForeground("msedge", "a", T0);
        tracker.SetPaused(true, T0.AddSeconds(40));
        Assert.False(tracker.OnForeground("discord", "b", T0.AddSeconds(60)));
        tracker.SetPaused(false, T0.AddSeconds(120));

        var d = Assert.Single(tracker.Finish(T0.AddSeconds(200)));
        Assert.Equal(40, d.DurationSeconds);
    }
}
