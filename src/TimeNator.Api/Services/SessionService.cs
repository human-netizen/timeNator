using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Api.Hubs;
using TimeNator.Api.Validation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public record SessionCreateResult(SessionResponse? Session, List<RuleViolation> Violations);

public class SessionService(
    AppDbContext db,
    LeaderboardService leaderboard,
    IHubContext<StudyHub, IStudyClient> hub,
    TimeProvider clock)
{
    public Task<List<SessionResponse>> ListAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        // Npgsql only writes UTC to timestamptz, so normalise whatever offset the client sent.
        from = from.ToUniversalTime();
        to = to.ToUniversalTime();
        return db.StudySessions
            .Where(s => s.UserId == userId && s.StartedAt >= from && s.StartedAt < to)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new SessionResponse(
                s.Id, s.SubjectId, s.Subject.Name, s.Subject.ColorHex, s.StartedAt, s.EndedAt,
                s.DurationSeconds, s.PausedSeconds, s.Mode, s.Source, s.MaxStreakSeconds))
            .ToListAsync(cancellationToken);
    }

    public async Task<SessionCreateResult> CreateAsync(
        Guid userId, CreateSessionRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            StartedAt = request.StartedAt.ToUniversalTime(),
            EndedAt = request.EndedAt.ToUniversalTime()
        };

        var subject = await db.Subjects
            .SingleOrDefaultAsync(s => s.Id == request.SubjectId && s.UserId == userId, cancellationToken);
        if (subject is null)
            return new(null, [new(nameof(request.SubjectId), "Unknown subject.")]);

        var overlapping = await db.StudySessions
            .Where(s => s.UserId == userId && s.StartedAt < request.EndedAt && s.EndedAt > request.StartedAt)
            .Select(s => new TimeRange(s.StartedAt, s.EndedAt))
            .ToListAsync(cancellationToken);

        var timing = new SessionTiming(request.StartedAt, request.EndedAt, request.DurationSeconds,
            request.PausedSeconds);
        var violations = SessionRules.Check(timing, clock.GetUtcNow(), overlapping);
        if (request.MaxStreakSeconds < 0 || request.MaxStreakSeconds > request.DurationSeconds)
            violations.Add(new(nameof(request.MaxStreakSeconds), "Streak must be between zero and the duration."));
        if (violations.Count > 0)
            return new(null, violations);

        var session = new StudySession
        {
            UserId = userId,
            SubjectId = subject.Id,
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
            DurationSeconds = request.DurationSeconds,
            PausedSeconds = request.PausedSeconds,
            Mode = request.Mode,
            Source = request.Source,
            MaxStreakSeconds = request.MaxStreakSeconds,
            CreatedAt = clock.GetUtcNow()
        };
        db.StudySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await UpdateLeaderboardAsync(userId, session, cancellationToken);

        return new(new SessionResponse(
            session.Id, subject.Id, subject.Name, subject.ColorHex, session.StartedAt, session.EndedAt,
            session.DurationSeconds, session.PausedSeconds, session.Mode, session.Source,
            session.MaxStreakSeconds), []);
    }

    /// <summary>
    /// Credits the session to its day and, when that changes today's visible top 50,
    /// pushes the new board to clients that have it open. Once there are more than fifty
    /// users, most saves change nothing visible and so broadcast nothing.
    /// </summary>
    private async Task UpdateLeaderboardAsync(Guid userId, StudySession session, CancellationToken cancellationToken)
    {
        var day = LeaderboardService.DayOf(session.StartedAt);
        var changed = await leaderboard.RecordAsync(userId, day, session.DurationSeconds, cancellationToken);
        if (changed && day == LeaderboardService.DayOf(clock.GetUtcNow()))
            await hub.Clients.Group(StudyHub.LeaderboardGroup)
                .LeaderboardUpdated(await leaderboard.GetTopAsync(day, cancellationToken));
    }
}
