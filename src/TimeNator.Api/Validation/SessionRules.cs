namespace TimeNator.Api.Validation;

public record SessionTiming(DateTimeOffset StartedAt, DateTimeOffset EndedAt, int DurationSeconds, int PausedSeconds);

public record TimeRange(DateTimeOffset Start, DateTimeOffset End);

public record RuleViolation(string Field, string Message);

/// <summary>
/// Sanity bounds on a client-reported session. The client owns the clock, so these
/// do not prove the time was spent; they keep bad or forged data within reason.
/// </summary>
public static class SessionRules
{
    public const int MaxDurationSeconds = 24 * 60 * 60;
    public static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(5);

    public static List<RuleViolation> Check(
        SessionTiming session, DateTimeOffset now, IEnumerable<TimeRange> otherSessions)
    {
        var violations = new List<RuleViolation>();

        if (session.DurationSeconds <= 0)
            violations.Add(new(nameof(session.DurationSeconds), "Duration must be positive."));
        else if (session.DurationSeconds > MaxDurationSeconds)
            violations.Add(new(nameof(session.DurationSeconds), "Duration must be at most 24 hours."));

        if (session.PausedSeconds < 0)
            violations.Add(new(nameof(session.PausedSeconds), "Paused time cannot be negative."));

        if (session.StartedAt > now)
            violations.Add(new(nameof(session.StartedAt), "A session cannot start in the future."));

        if (session.EndedAt > now + Tolerance)
            violations.Add(new(nameof(session.EndedAt), "A session cannot end in the future."));

        var expectedEnd = session.StartedAt.AddSeconds(session.DurationSeconds + session.PausedSeconds);
        if ((session.EndedAt - expectedEnd).Duration() > Tolerance)
            violations.Add(new(nameof(session.EndedAt), "End must equal start plus duration plus paused time."));

        if (session.EndedAt > session.StartedAt &&
            otherSessions.Any(o => session.StartedAt < o.End && o.Start < session.EndedAt))
            violations.Add(new(nameof(session.StartedAt), "The session overlaps another session."));

        return violations;
    }
}
