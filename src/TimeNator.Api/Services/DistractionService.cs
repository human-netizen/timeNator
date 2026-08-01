using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class DistractionService(AppDbContext db)
{
    public const int MaxBatch = 500;

    /// <summary>
    /// Stores a batch sent when a session ends. A session id that is not the caller's own
    /// is dropped rather than trusted, so nobody can attach events to another user's session.
    /// </summary>
    public async Task<ServiceResult<int>> AddBatchAsync(Guid userId, List<DistractionEventRequest> events,
        CancellationToken cancellationToken)
    {
        if (events.Count is 0 or > MaxBatch)
            return ServiceResult<int>.Fail(ServiceError.Invalid, $"Send between 1 and {MaxBatch} events.");
        if (events.Any(e => e.DurationSeconds is < 0 or > 24 * 60 * 60 || string.IsNullOrWhiteSpace(e.ProcessName)))
            return ServiceResult<int>.Fail(ServiceError.Invalid, "Each event needs a process and a sane duration.");

        var claimed = events.Where(e => e.SessionId is not null).Select(e => e.SessionId!.Value).Distinct().ToList();
        var owned = await db.StudySessions
            .Where(s => s.UserId == userId && claimed.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        db.DistractionEvents.AddRange(events.Select(e => new DistractionEvent
        {
            UserId = userId,
            SessionId = e.SessionId is { } id && owned.Contains(id) ? id : null,
            ProcessName = Truncate(e.ProcessName.Trim().ToLowerInvariant(), 100),
            WindowTitle = Truncate(e.WindowTitle, 300),
            OccurredAt = e.OccurredAt.ToUniversalTime(),
            DurationSeconds = e.DurationSeconds
        }));
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Ok(events.Count);
    }

    public Task<List<DistractionEventResponse>> ListAsync(Guid userId, DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        from = from.ToUniversalTime();
        to = to.ToUniversalTime();
        return db.DistractionEvents
            .Where(e => e.UserId == userId && e.OccurredAt >= from && e.OccurredAt < to)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new DistractionEventResponse(e.Id, e.SessionId, e.ProcessName, e.WindowTitle, e.OccurredAt,
                e.DurationSeconds))
            .ToListAsync(cancellationToken);
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];
}
