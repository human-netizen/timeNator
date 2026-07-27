using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TimeNator.Api.Data;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

/// <summary>
/// The one leaderboard: every user, ranked by seconds studied on a UTC day, top 50.
/// A Redis sorted set per day holds user ids and scores only; names come from Postgres
/// by primary key. Redis is a cache here: on a miss, or if Redis is down, the day is
/// recomputed from the sessions table.
/// </summary>
public class LeaderboardService(IConnectionMultiplexer redis, AppDbContext db, ILogger<LeaderboardService> log)
{
    public const int Size = 50;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(48);

    public static DateOnly DayOf(DateTimeOffset instant) => DateOnly.FromDateTime(instant.UtcDateTime);

    private static RedisKey Key(DateOnly day) => $"leaderboard:global:{day:yyyy-MM-dd}";

    /// <summary>Adds a saved session to its day. Returns true when the visible top 50 changed.</summary>
    public async Task<bool> RecordAsync(Guid userId, DateOnly day, int seconds, CancellationToken cancellationToken)
    {
        try
        {
            var cache = redis.GetDatabase();
            var key = Key(day);

            // If the key is missing, incrementing would create a set holding only this user.
            // Rebuild instead; the session is already committed, so the rebuild includes it.
            if (!await cache.KeyExistsAsync(key))
                await RebuildAsync(day, cancellationToken);
            else
                await cache.SortedSetIncrementAsync(key, userId.ToString(), seconds);

            var rank = await cache.SortedSetRankAsync(key, userId.ToString(), Order.Descending);
            return rank is < Size;
        }
        catch (RedisException ex)
        {
            log.LogWarning(ex, "Leaderboard update skipped; Redis unavailable");
            return false;
        }
    }

    public async Task<List<LeaderboardEntry>> GetTopAsync(DateOnly day, CancellationToken cancellationToken)
    {
        List<(Guid UserId, int Seconds)> top;
        try
        {
            var cache = redis.GetDatabase();
            var key = Key(day);
            if (!await cache.KeyExistsAsync(key))
                await RebuildAsync(day, cancellationToken);

            var entries = await cache.SortedSetRangeByRankWithScoresAsync(key, 0, Size - 1, Order.Descending);
            top = entries.Select(e => (Guid.Parse(e.Element.ToString()), (int)e.Score)).ToList();
        }
        catch (RedisException ex)
        {
            log.LogWarning(ex, "Leaderboard read from Postgres; Redis unavailable");
            top = (await ComputeAsync(day, cancellationToken))
                .OrderByDescending(t => t.Seconds).ThenByDescending(t => t.UserId.ToString())
                .Take(Size).ToList();
        }

        var ids = top.Select(t => t.UserId).ToList();
        var users = await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.AvatarKey })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return top
            .Where(t => users.ContainsKey(t.UserId))
            .Select((t, i) => new LeaderboardEntry(t.UserId, users[t.UserId].DisplayName, users[t.UserId].AvatarKey,
                t.Seconds, i + 1))
            .ToList();
    }

    private async Task RebuildAsync(DateOnly day, CancellationToken cancellationToken)
    {
        var totals = await ComputeAsync(day, cancellationToken);
        if (totals.Count == 0)
            return;

        var cache = redis.GetDatabase();
        var key = Key(day);
        var entries = totals.Select(t => new SortedSetEntry(t.UserId.ToString(), t.Seconds)).ToArray();
        var transaction = cache.CreateTransaction();
        _ = transaction.KeyDeleteAsync(key);
        _ = transaction.SortedSetAddAsync(key, entries);
        _ = transaction.KeyExpireAsync(key, Ttl);
        await transaction.ExecuteAsync();
    }

    private async Task<List<(Guid UserId, int Seconds)>> ComputeAsync(DateOnly day,
        CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);
        var totals = await db.StudySessions
            .Where(s => s.StartedAt >= start && s.StartedAt < end)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Seconds = g.Sum(s => s.DurationSeconds) })
            .ToListAsync(cancellationToken);
        return totals.Select(t => (t.UserId, t.Seconds)).ToList();
    }
}
