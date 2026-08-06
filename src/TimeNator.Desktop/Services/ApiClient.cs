using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

/// <summary>A failed API call. <see cref="StatusCode"/> is null when the server could not be reached.</summary>
public class ApiException(string message, int? statusCode) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}

public class ApiClient(HttpClient http) : IApiClient
{
    public Task<HealthReportResponse> GetHealthAsync(CancellationToken cancellationToken = default) =>
        GetAsync<HealthReportResponse>("health", cancellationToken);

    public Task<List<SubjectResponse>> GetSubjectsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<SubjectResponse>>("api/subjects", cancellationToken);

    public Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(HttpMethod.Post, "api/subjects", request, cancellationToken);

    public Task<SubjectResponse> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(HttpMethod.Put, $"api/subjects/{id}", request, cancellationToken);

    public Task DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/subjects/{id}", null, cancellationToken);

    public Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<SessionResponse>(HttpMethod.Post, "api/sessions", request, cancellationToken);

    public Task<List<SessionResponse>> GetSessionsAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<SessionResponse>>(
            $"api/sessions?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}",
            cancellationToken);

    public Task<List<DayOffResponse>> GetDayOffsAsync(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<DayOffResponse>>($"api/day-offs?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", cancellationToken);

    public Task<DayOffResponse> CreateDayOffAsync(DayOffRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<DayOffResponse>(HttpMethod.Post, "api/day-offs", request, cancellationToken);

    public Task DeleteDayOffAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/day-offs/{date:yyyy-MM-dd}", null, cancellationToken);

    public Task<List<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<GroupSummary>>("api/groups/mine", cancellationToken);

    public Task<PagedResult<GroupSummary>> SearchGroupsAsync(string query, int page,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<GroupSummary>>($"api/groups/search?q={Uri.EscapeDataString(query)}&page={page}",
            cancellationToken);

    public Task<GroupDetail> GetGroupAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<GroupDetail>($"api/groups/{id}", cancellationToken);

    public Task<GroupDetail> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<GroupDetail>(HttpMethod.Post, "api/groups", request, cancellationToken);

    public Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/groups/{id}", null, cancellationToken);

    public Task<GroupDetail> JoinGroupAsync(Guid id, string? password, CancellationToken cancellationToken = default) =>
        SendAsync<GroupDetail>(HttpMethod.Post, $"api/groups/{id}/join", new JoinGroupRequest(password),
            cancellationToken);

    public Task LeaveGroupAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/groups/{id}/leave", null, cancellationToken);

    public Task<List<GroupMessageItem>> GetGroupMessagesAsync(Guid groupId, Guid? before,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<GroupMessageItem>>(
            before is { } cursor ? $"api/groups/{groupId}/messages?before={cursor}" : $"api/groups/{groupId}/messages",
            cancellationToken);

    public Task<GroupDetail> UpdateGroupAsync(Guid id, UpdateGroupRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<GroupDetail>(HttpMethod.Put, $"api/groups/{id}", request, cancellationToken);

    public Task SetChatPermissionAsync(Guid groupId, Guid userId, bool canChat,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/groups/{groupId}/members/{userId}/chat-permission",
            new ChatPermissionRequest(canChat), cancellationToken);

    public Task KickAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/groups/{groupId}/members/{userId}/kick", null, cancellationToken);

    public Task BlacklistAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/groups/{groupId}/members/{userId}/blacklist", new RemoveMemberRequest(null),
            cancellationToken);

    public Task<List<AllowedAppResponse>> GetAllowedAppsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<AllowedAppResponse>>("api/allowed-apps", cancellationToken);

    public Task<AllowedAppResponse> AddAllowedAppAsync(AllowedAppRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<AllowedAppResponse>(HttpMethod.Post, "api/allowed-apps", request, cancellationToken);

    public Task RemoveAllowedAppAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/allowed-apps/{id}", null, cancellationToken);

    public Task AddDistractionEventsAsync(List<DistractionEventRequest> events,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/distraction-events", events, cancellationToken);

    public Task<List<DistractionEventResponse>> GetDistractionEventsAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<DistractionEventResponse>>(
            $"api/distraction-events?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}",
            cancellationToken);

    public Task<DailyReviewResponse> GetDailyReviewAsync(DateOnly date, TimeSpan utcOffset,
        CancellationToken cancellationToken = default) =>
        GetAsync<DailyReviewResponse>(
            $"api/daily-review?date={date:yyyy-MM-dd}&offsetMinutes={(int)utcOffset.TotalMinutes}", cancellationToken);

    public Task<TodoResponse> CreateTodoAsync(CreateTodoRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<TodoResponse>(HttpMethod.Post, "api/todos", request, cancellationToken);

    public Task<TodoResponse> UpdateTodoAsync(Guid id, UpdateTodoRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<TodoResponse>(HttpMethod.Put, $"api/todos/{id}", request, cancellationToken);

    public Task DeleteTodoAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/todos/{id}", null, cancellationToken);

    public Task<List<DdayResponse>> GetDdaysAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<DdayResponse>>("api/ddays", cancellationToken);

    public Task<DdayResponse> CreateDdayAsync(DdayRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<DdayResponse>(HttpMethod.Post, "api/ddays", request, cancellationToken);

    public Task DeleteDdayAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/ddays/{id}", null, cancellationToken);

    public Task<List<TimetableResponse>> GetTimetableAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<TimetableResponse>>("api/timetable", cancellationToken);

    public Task<TimetableResponse> CreateTimetableEntryAsync(TimetableRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<TimetableResponse>(HttpMethod.Post, "api/timetable", request, cancellationToken);

    public Task DeleteTimetableEntryAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/timetable/{id}", null, cancellationToken);

    public Task<List<LeaderboardEntry>> GetLeaderboardAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<LeaderboardEntry>>("api/leaderboard", cancellationToken);

    public Task<List<MemberPresence>> GetGroupPresenceAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<List<MemberPresence>>($"api/groups/{id}/presence", cancellationToken);

    public Task<List<GroupMemberItem>> GetGroupMembersAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<List<GroupMemberItem>>($"api/groups/{id}/members", cancellationToken);

    public Task<InviteResponse> CreateInviteAsync(Guid groupId, CreateInviteRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<InviteResponse>(HttpMethod.Post, $"api/groups/{groupId}/invites", request, cancellationToken);

    public Task<GroupDetail> AcceptInviteAsync(string code, CancellationToken cancellationToken = default) =>
        SendAsync<GroupDetail>(HttpMethod.Post, $"api/groups/join/{Uri.EscapeDataString(code)}", null,
            cancellationToken);

    private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, path, body, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
               ?? throw new ApiException("The server returned an empty response.", (int)response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType());

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("The server could not be reached.", null);
        }

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
            throw new ApiException(await ProblemReader.ReadMessageAsync(response, cancellationToken),
                (int)response.StatusCode);
    }
}
