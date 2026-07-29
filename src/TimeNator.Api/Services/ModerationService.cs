using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Api.Hubs;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

/// <summary>
/// Owner-only group management. Ownership is checked inside each query against the
/// caller's id from the token, never against anything the request body claims.
/// </summary>
public class ModerationService(
    AppDbContext db,
    GroupService groups,
    IHubContext<StudyHub, IStudyClient> hub,
    TimeProvider clock)
{
    public async Task<ServiceResult<GroupDetail>> UpdateAsync(Guid ownerId, Guid groupId, UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var group = await db.Groups.SingleOrDefaultAsync(g => g.Id == groupId && g.OwnerId == ownerId,
            cancellationToken);
        if (group is null)
            return NotOwner<GroupDetail>();

        group.Name = request.Name.Trim();
        group.Description = Clean(request.Description);
        group.Announcement = Clean(request.Announcement);
        group.ChatEnabled = request.ChatEnabled;
        group.MinDailySeconds = request.MinDailySeconds;
        await db.SaveChangesAsync(cancellationToken);

        await hub.Clients.Group(StudyHub.GroupName(groupId)).GroupUpdated(groupId);
        return await groups.GetAsync(ownerId, groupId, cancellationToken);
    }

    public async Task<ServiceResult<bool>> SetChatPermissionAsync(Guid ownerId, Guid groupId, Guid userId,
        bool canChat, CancellationToken cancellationToken)
    {
        var updated = await db.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == userId && m.UserId != ownerId
                        && m.Group.OwnerId == ownerId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.CanChat, canChat), cancellationToken);
        if (updated == 0)
            return NotOwner<bool>();

        await hub.Clients.Group(StudyHub.GroupName(groupId)).GroupUpdated(groupId);
        return ServiceResult<bool>.Ok(true);
    }

    /// <summary>Removes a member; with <paramref name="blacklist"/> they also cannot come back.</summary>
    public async Task<ServiceResult<bool>> RemoveAsync(Guid ownerId, Guid groupId, Guid userId, bool blacklist,
        string? reason, CancellationToken cancellationToken)
    {
        if (userId == ownerId || !await db.Groups.AnyAsync(g => g.Id == groupId && g.OwnerId == ownerId,
                cancellationToken))
            return NotOwner<bool>();

        var removed = await db.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        if (blacklist && !await db.GroupBlacklist.AnyAsync(b => b.GroupId == groupId && b.UserId == userId,
                cancellationToken))
        {
            db.GroupBlacklist.Add(new GroupBlacklistEntry
            {
                GroupId = groupId,
                UserId = userId,
                BlacklistedAt = clock.GetUtcNow(),
                Reason = Clean(reason)
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (removed == 0 && !blacklist)
            return ServiceResult<bool>.Fail(ServiceError.NotFound, "That user is not a member.");

        await hub.Clients.Group(StudyHub.GroupName(groupId)).MemberRemoved(groupId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    private static ServiceResult<T> NotOwner<T>() =>
        ServiceResult<T>.Fail(ServiceError.NotFound, "Group or member not found, or you are not the owner.");

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
