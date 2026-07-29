using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

/// <summary>Authenticated calls to the API. Failures surface as <see cref="ApiException"/>.</summary>
public interface IApiClient
{
    Task<HealthReportResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<List<SubjectResponse>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken = default);
    Task<SubjectResponse> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request,
        CancellationToken cancellationToken = default);
    Task DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<List<SessionResponse>> GetSessionsAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<List<DayOffResponse>> GetDayOffsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<DayOffResponse> CreateDayOffAsync(DayOffRequest request, CancellationToken cancellationToken = default);
    Task DeleteDayOffAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<List<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<GroupSummary>> SearchGroupsAsync(string query, int page,
        CancellationToken cancellationToken = default);
    Task<GroupDetail> GetGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupDetail> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupDetail> JoinGroupAsync(Guid id, string? password, CancellationToken cancellationToken = default);
    Task LeaveGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<GroupMemberItem>> GetGroupMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MemberPresence>> GetGroupPresenceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InviteResponse> CreateInviteAsync(Guid groupId, CreateInviteRequest request,
        CancellationToken cancellationToken = default);
    Task<GroupDetail> AcceptInviteAsync(string code, CancellationToken cancellationToken = default);

    Task<List<GroupMessageItem>> GetGroupMessagesAsync(Guid groupId, Guid? before,
        CancellationToken cancellationToken = default);

    Task<List<LeaderboardEntry>> GetLeaderboardAsync(CancellationToken cancellationToken = default);
}
