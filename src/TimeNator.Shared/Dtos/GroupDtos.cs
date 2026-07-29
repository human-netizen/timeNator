namespace TimeNator.Shared.Dtos;

public record CreateGroupRequest(string Name, string? Description, bool IsPublic, string? Password);

public record GroupSummary(
    Guid Id,
    string Name,
    string? Description,
    bool IsPublic,
    int MemberCount,
    string OwnerDisplayName);

public record GroupDetail(
    Guid Id,
    string Name,
    string? Description,
    bool IsPublic,
    string? Announcement,
    bool ChatEnabled,
    int? MinDailySeconds,
    Guid OwnerId,
    int MemberCount,
    GroupRole? MyRole);

public record JoinGroupRequest(string? Password);

public record GroupMemberItem(
    Guid UserId,
    string DisplayName,
    string? AvatarKey,
    GroupRole Role,
    bool CanChat,
    DateTimeOffset JoinedAt);

public record CreateInviteRequest(int? ExpiresInHours, int? MaxUses);

public record InviteResponse(string Code, DateTimeOffset? ExpiresAt, int? MaxUses, int UseCount);

public record GroupMessageItem(Guid Id, Guid GroupId, Guid UserId, string DisplayName, string Body, DateTimeOffset SentAt);
