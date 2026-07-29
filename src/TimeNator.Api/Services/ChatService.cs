using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Data.Configurations;
using TimeNator.Api.Entities;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class ChatService(AppDbContext db, TimeProvider clock)
{
    public const int PageSize = 50;

    /// <summary>
    /// Saves a message if the sender may post: a member, in a group with chat switched on,
    /// who has not had posting rights taken away. The owner can always post.
    /// </summary>
    public async Task<ServiceResult<GroupMessageItem>> SendAsync(Guid userId, Guid groupId, string body,
        CancellationToken cancellationToken)
    {
        body = body.Trim();
        if (body.Length is 0 or > GroupMessageRules.MaxLength)
            return ServiceResult<GroupMessageItem>.Fail(ServiceError.Invalid,
                $"A message must be 1 to {GroupMessageRules.MaxLength} characters.");

        var sender = await db.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == userId)
            .Select(m => new { m.Role, m.CanChat, m.Group.ChatEnabled, m.User.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (sender is null)
            return ServiceResult<GroupMessageItem>.Fail(ServiceError.NotFound, "Group not found.");
        if (sender.Role != GroupRole.Owner && (!sender.ChatEnabled || !sender.CanChat))
            return ServiceResult<GroupMessageItem>.Fail(ServiceError.Forbidden,
                sender.ChatEnabled ? "You may not post in this group." : "Chat is switched off in this group.");

        var message = new GroupMessage { GroupId = groupId, UserId = userId, Body = body, SentAt = clock.GetUtcNow() };
        db.GroupMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<GroupMessageItem>.Ok(new GroupMessageItem(message.Id, groupId, userId,
            sender.DisplayName, body, message.SentAt));
    }

    /// <summary>
    /// One page of history, newest first, older than <paramref name="before"/> when given.
    /// Message ids are version 7 GUIDs, so they sort by creation time and make a stable cursor.
    /// </summary>
    public async Task<ServiceResult<List<GroupMessageItem>>> HistoryAsync(Guid userId, Guid groupId, Guid? before,
        CancellationToken cancellationToken)
    {
        if (!await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken))
            return ServiceResult<List<GroupMessageItem>>.Fail(ServiceError.NotFound, "Group not found.");

        var query = db.GroupMessages.Where(m => m.GroupId == groupId);
        if (before is { } cursor)
        {
            var cursorTime = await db.GroupMessages.Where(m => m.Id == cursor).Select(m => (DateTimeOffset?)m.SentAt)
                .SingleOrDefaultAsync(cancellationToken);
            if (cursorTime is { } t)
                query = query.Where(m => m.SentAt < t || (m.SentAt == t && m.Id < cursor));
        }

        var page = await query
            .OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
            .Take(PageSize)
            .Select(m => new GroupMessageItem(m.Id, m.GroupId, m.UserId, m.User.DisplayName, m.Body, m.SentAt))
            .ToListAsync(cancellationToken);
        return ServiceResult<List<GroupMessageItem>>.Ok(page);
    }
}
