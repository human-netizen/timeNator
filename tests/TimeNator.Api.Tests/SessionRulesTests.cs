using TimeNator.Api.Validation;

namespace TimeNator.Api.Tests;

public class SessionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    private static SessionTiming Session(int startMinutesAgo, int durationSeconds, int pausedSeconds = 0)
    {
        var start = Now.AddMinutes(-startMinutesAgo);
        return new SessionTiming(start, start.AddSeconds(durationSeconds + pausedSeconds), durationSeconds,
            pausedSeconds);
    }

    [Fact]
    public void Valid_session_has_no_violations()
    {
        var violations = SessionRules.Check(Session(60, 1800, 300), Now, []);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(SessionRules.MaxDurationSeconds + 1)]
    public void Duration_out_of_bounds_is_rejected(int duration)
    {
        var violations = SessionRules.Check(Session(24 * 60 + 60, duration), Now, []);

        Assert.Contains(violations, v => v.Field == nameof(SessionTiming.DurationSeconds));
    }

    [Fact]
    public void Session_starting_in_the_future_is_rejected()
    {
        var violations = SessionRules.Check(Session(-10, 60), Now, []);

        Assert.Contains(violations, v => v.Field == nameof(SessionTiming.StartedAt));
    }

    [Fact]
    public void Inconsistent_end_is_rejected()
    {
        var session = Session(60, 1800) with { EndedAt = Now.AddMinutes(-60).AddSeconds(1900) };

        var violations = SessionRules.Check(session, Now, []);

        Assert.Contains(violations, v => v.Field == nameof(SessionTiming.EndedAt));
    }

    [Fact]
    public void End_within_tolerance_is_accepted()
    {
        var session = Session(60, 1800) with { EndedAt = Now.AddMinutes(-60).AddSeconds(1803) };

        Assert.Empty(SessionRules.Check(session, Now, []));
    }

    [Fact]
    public void Overlapping_session_is_rejected()
    {
        var existing = new TimeRange(Now.AddMinutes(-50), Now.AddMinutes(-40));

        var violations = SessionRules.Check(Session(60, 1800), Now, [existing]);

        Assert.Contains(violations, v => v.Message.Contains("overlaps"));
    }

    [Fact]
    public void Adjacent_session_is_not_an_overlap()
    {
        var existing = new TimeRange(Now.AddMinutes(-90), Now.AddMinutes(-60));

        Assert.Empty(SessionRules.Check(Session(60, 1800), Now, [existing]));
    }
}
