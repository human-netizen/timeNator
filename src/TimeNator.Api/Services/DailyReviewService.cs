using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

/// <summary>One day at a glance: what was studied, per subject, next to what was planned.</summary>
public class DailyReviewService(AppDbContext db, TodoService todos)
{
    /// <param name="utcOffset">
    /// The client's offset from UTC. A "day" is the user's local day, and the server stores
    /// only UTC instants, so the client says where its midnight falls.
    /// </param>
    public async Task<DailyReviewResponse> GetAsync(Guid userId, DateOnly date, TimeSpan utcOffset,
        CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), utcOffset).ToUniversalTime();
        var end = start.AddDays(1);

        var subjects = await db.StudySessions
            .Where(s => s.UserId == userId && s.StartedAt >= start && s.StartedAt < end)
            .GroupBy(s => new { s.SubjectId, s.Subject.Name, s.Subject.ColorHex })
            .Select(g => new
            {
                g.Key.SubjectId, g.Key.Name, g.Key.ColorHex,
                Seconds = g.Sum(s => s.DurationSeconds),
                Count = g.Count()
            })
            .OrderByDescending(g => g.Seconds)
            .ToListAsync(cancellationToken);

        var dayTodos = await todos.ListForDayAsync(userId, date, cancellationToken);

        return new DailyReviewResponse(
            date,
            subjects.Sum(s => s.Seconds),
            subjects.Sum(s => s.Count),
            subjects.Select(s => new SubjectTotal(s.SubjectId, s.Name, s.ColorHex, s.Seconds)).ToList(),
            dayTodos);
    }
}
