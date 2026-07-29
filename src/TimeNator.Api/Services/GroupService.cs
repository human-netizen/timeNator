using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class GroupService(AppDbContext db, IPasswordHasher<Group> hasher, TimeProvider clock)
{
    public async Task<ServiceResult<GroupDetail>> CreateAsync(
        Guid userId, CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var group = new Group
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsPublic = request.IsPublic,
            OwnerId = userId,
            CreatedAt = now
        };
        if (!request.IsPublic)
            group.PasswordHash = hasher.HashPassword(group, request.Password!);

        group.Members.Add(new GroupMember { UserId = userId, Role = GroupRole.Owner, JoinedAt = now });
        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<GroupDetail>.Ok(ToDetail(group, 1, GroupRole.Owner));
    }

    /// <summary>Members see any group they belong to; everyone else sees public groups only.</summary>
    public async Task<ServiceResult<GroupDetail>> GetAsync(Guid userId, Guid groupId,
        CancellationToken cancellationToken)
    {
        var found = await db.Groups
            .Where(g => g.Id == groupId)
            .Select(g => new
            {
                Group = g,
                MemberCount = g.Members.Count,
                MyRole = g.Members.Where(m => m.UserId == userId).Select(m => (GroupRole?)m.Role).FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (found is null || (found.MyRole is null && !found.Group.IsPublic))
            return ServiceResult<GroupDetail>.Fail(ServiceError.NotFound, "Group not found.");

        return ServiceResult<GroupDetail>.Ok(ToDetail(found.Group, found.MemberCount, found.MyRole));
    }

    public Task<List<GroupSummary>> ListMineAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderBy(g => g.Name)
            .Select(g => new GroupSummary(g.Id, g.Name, g.Description, g.IsPublic, g.Members.Count,
                g.Owner.DisplayName))
            .ToListAsync(cancellationToken);

    public const int SearchPageSize = 20;

    /// <summary>Public groups whose name contains the query, case-insensitively. Private groups never appear.</summary>
    public async Task<PagedResult<GroupSummary>> SearchAsync(string? query, int page,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        var groups = db.Groups.Where(g => g.IsPublic);
        if (!string.IsNullOrWhiteSpace(query))
        {
            // Escape LIKE wildcards so a user typing % or _ searches for them literally.
            var pattern = "%" + query.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            groups = groups.Where(g => EF.Functions.ILike(g.Name, pattern));
        }

        var total = await groups.CountAsync(cancellationToken);
        var items = await groups
            .OrderByDescending(g => g.Members.Count).ThenBy(g => g.Name)
            .Skip((page - 1) * SearchPageSize)
            .Take(SearchPageSize)
            .Select(g => new GroupSummary(g.Id, g.Name, g.Description, g.IsPublic, g.Members.Count,
                g.Owner.DisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResult<GroupSummary>(items, page, SearchPageSize, total);
    }

    public async Task<ServiceResult<GroupDetail>> JoinAsync(Guid userId, Guid groupId, string? password,
        CancellationToken cancellationToken)
    {
        var group = await db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
            return ServiceResult<GroupDetail>.Fail(ServiceError.NotFound, "Group not found.");

        if (!group.IsPublic &&
            (password is null || hasher.VerifyHashedPassword(group, group.PasswordHash!, password)
                == PasswordVerificationResult.Failed))
            return ServiceResult<GroupDetail>.Fail(ServiceError.Forbidden, "Wrong group password.");

        return await AddMemberAsync(userId, group, cancellationToken);
    }

    /// <summary>Adds a member without any password check; callers decide whether that is allowed.</summary>
    public async Task<ServiceResult<GroupDetail>> AddMemberAsync(Guid userId, Group group,
        CancellationToken cancellationToken)
    {
        if (await db.GroupBlacklist.AnyAsync(b => b.GroupId == group.Id && b.UserId == userId, cancellationToken))
            return ServiceResult<GroupDetail>.Fail(ServiceError.Forbidden, "You have been barred from this group.");
        if (await db.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == userId, cancellationToken))
            return ServiceResult<GroupDetail>.Fail(ServiceError.Conflict, "You are already a member.");

        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRole.Member,
            JoinedAt = clock.GetUtcNow()
        });
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, group.Id, cancellationToken);
    }

    public async Task<ServiceResult<bool>> LeaveAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        var member = await db.GroupMembers
            .SingleOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
        if (member is null)
            return ServiceResult<bool>.Fail(ServiceError.NotFound, "You are not a member of this group.");
        if (member.Role == GroupRole.Owner)
            return ServiceResult<bool>.Fail(ServiceError.Invalid,
                "The owner cannot leave. Delete the group instead.");

        db.GroupMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    /// <summary>Owner only. Members are dropped with the group.</summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        var deleted = await db.Groups
            .Where(g => g.Id == groupId && g.OwnerId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0
            ? ServiceResult<bool>.Ok(true)
            : ServiceResult<bool>.Fail(ServiceError.NotFound, "Group not found or you are not its owner.");
    }

    public async Task<ServiceResult<List<GroupMemberItem>>> ListMembersAsync(Guid userId, Guid groupId,
        CancellationToken cancellationToken)
    {
        if (!await IsMemberAsync(userId, groupId, cancellationToken))
            return ServiceResult<List<GroupMemberItem>>.Fail(ServiceError.NotFound, "Group not found.");

        // Today's seconds per member, for the group's minimum daily requirement (UTC day, as the leaderboard).
        var dayStart = new DateTimeOffset(clock.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero);
        var members = await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.Role).ThenBy(m => m.User.DisplayName)
            .Select(m => new GroupMemberItem(m.UserId, m.User.DisplayName, m.User.AvatarKey, m.Role, m.CanChat,
                m.JoinedAt,
                db.StudySessions
                    .Where(s => s.UserId == m.UserId && s.StartedAt >= dayStart)
                    .Sum(s => s.DurationSeconds)))
            .ToListAsync(cancellationToken);
        return ServiceResult<List<GroupMemberItem>>.Ok(members);
    }

    public Task<List<Guid>> ListMyGroupIdsAsync(Guid userId, CancellationToken cancellationToken) =>
        db.GroupMembers.Where(m => m.UserId == userId).Select(m => m.GroupId).ToListAsync(cancellationToken);

    public async Task<ServiceResult<List<Guid>>> ListMemberIdsAsync(Guid userId, Guid groupId,
        CancellationToken cancellationToken)
    {
        if (!await IsMemberAsync(userId, groupId, cancellationToken))
            return ServiceResult<List<Guid>>.Fail(ServiceError.NotFound, "Group not found.");
        return ServiceResult<List<Guid>>.Ok(await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken));
    }

    public Task<bool> IsMemberAsync(Guid userId, Guid groupId, CancellationToken cancellationToken) =>
        db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

    private static GroupDetail ToDetail(Group g, int memberCount, GroupRole? myRole) =>
        new(g.Id, g.Name, g.Description, g.IsPublic, g.Announcement, g.ChatEnabled, g.MinDailySeconds, g.OwnerId,
            memberCount, myRole);
}
