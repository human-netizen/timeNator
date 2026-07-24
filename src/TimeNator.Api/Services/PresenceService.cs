using StackExchange.Redis;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

/// <summary>
/// Who is studying right now, one Redis hash per user. Written when a timer starts and
/// deleted when it stops or the connection drops. The TTL is only a safety net for a
/// server that dies before it can run disconnect handling.
/// </summary>
public class PresenceService(IConnectionMultiplexer redis)
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private IDatabase Db => redis.GetDatabase();

    private static RedisKey Key(Guid userId) => $"presence:{userId}";

    public async Task SetAsync(MemberPresence presence)
    {
        var key = Key(presence.UserId);
        var batch = Db.CreateBatch();
        var write = batch.HashSetAsync(key,
        [
            new("displayName", presence.DisplayName),
            new("subjectId", presence.SubjectId.ToString()),
            new("subjectName", presence.SubjectName),
            new("subjectColor", presence.SubjectColorHex),
            new("startedAt", presence.StartedAt.ToUnixTimeMilliseconds()),
            new("mode", presence.Mode.ToString())
        ]);
        var expire = batch.KeyExpireAsync(key, Ttl);
        batch.Execute();
        await Task.WhenAll(write, expire);
    }

    public Task ClearAsync(Guid userId) => Db.KeyDeleteAsync(Key(userId));

    /// <summary>Pushes the safety-net expiry back; called on any hub activity from the user.</summary>
    public Task RefreshAsync(Guid userId) => Db.KeyExpireAsync(Key(userId), Ttl);

    public async Task<List<MemberPresence>> GetManyAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.ToList();
        var batch = Db.CreateBatch();
        var reads = ids.Select(id => batch.HashGetAllAsync(Key(id))).ToList();
        batch.Execute();
        await Task.WhenAll(reads);

        var result = new List<MemberPresence>();
        for (var i = 0; i < ids.Count; i++)
        {
            var fields = reads[i].Result.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
            if (fields.Count == 0)
                continue;
            result.Add(new MemberPresence(
                ids[i],
                fields["displayName"],
                Guid.Parse(fields["subjectId"]),
                fields["subjectName"],
                fields["subjectColor"],
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(fields["startedAt"])),
                Enum.Parse<SessionMode>(fields["mode"])));
        }
        return result;
    }
}
