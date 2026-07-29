using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public interface IStudyHubClient
{
    event Action<Guid, MemberPresence>? MemberStarted;
    event Action<Guid, Guid>? MemberStopped;
    event Action<List<LeaderboardEntry>>? LeaderboardUpdated;
    event Action<Guid, GroupMessageItem>? MessageReceived;
    event Action<Guid>? GroupUpdated;
    event Action<Guid, Guid>? MemberRemoved;

    Task ConnectAsync();
    Task DisconnectAsync();
    Task StartedStudyingAsync(Guid subjectId, SessionMode mode);
    Task StoppedStudyingAsync();
    Task KeepPresenceAsync();
    Task JoinMyGroupsAsync();
    Task SubscribeLeaderboardAsync();
    Task UnsubscribeLeaderboardAsync();

    /// <summary>Throws <see cref="HubException"/> with a readable reason when the server refuses.</summary>
    Task SendMessageAsync(Guid groupId, string body);
    Task LeaveGroupChannelAsync(Guid groupId);
}

/// <summary>
/// The desktop end of /hubs/study. Reconnects on its own after a drop, and because the
/// server clears presence on disconnect, re-announces a running timer once it is back.
/// Events fire on a background thread; subscribers marshal to the UI thread.
/// Calls made while disconnected are dropped rather than queued: presence is live state,
/// and a stale announcement is worse than a missing one.
/// </summary>
public class StudyHubClient : IStudyHubClient
{
    private readonly HubConnection _connection;
    private (Guid SubjectId, SessionMode Mode)? _studying;
    private bool _leaderboardOpen;

    public StudyHubClient(Uri apiBase, IAuthService auth)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(apiBase, "hubs/study"), options =>
                options.AccessTokenProvider = () => auth.GetAccessTokenAsync())
            .WithAutomaticReconnect()
            .Build();

        _connection.On<Guid, MemberPresence>("MemberStarted", (g, p) => MemberStarted?.Invoke(g, p));
        _connection.On<Guid, Guid>("MemberStopped", (g, u) => MemberStopped?.Invoke(g, u));
        _connection.On<List<LeaderboardEntry>>("LeaderboardUpdated", e => LeaderboardUpdated?.Invoke(e));
        _connection.On<Guid, GroupMessageItem>("MessageReceived", (g, m) => MessageReceived?.Invoke(g, m));
        _connection.On<Guid>("GroupUpdated", g => GroupUpdated?.Invoke(g));
        _connection.On<Guid, Guid>("MemberRemoved", (g, u) => MemberRemoved?.Invoke(g, u));
        _connection.Reconnected += async _ =>
        {
            if (_studying is { } s)
                await SendAsync("StartedStudying", s.SubjectId, s.Mode);
            if (_leaderboardOpen)
                await SendAsync("SubscribeLeaderboard");
        };
    }

    public event Action<Guid, MemberPresence>? MemberStarted;
    public event Action<Guid, Guid>? MemberStopped;
    public event Action<List<LeaderboardEntry>>? LeaderboardUpdated;
    public event Action<Guid, GroupMessageItem>? MessageReceived;
    public event Action<Guid>? GroupUpdated;
    public event Action<Guid, Guid>? MemberRemoved;

    public async Task ConnectAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected)
            return;
        try
        {
            await _connection.StartAsync();
        }
        catch (HttpRequestException)
        {
            // The API is down. Live features stay quiet; the timer and REST retries still work.
        }
    }

    public async Task DisconnectAsync()
    {
        _studying = null;
        _leaderboardOpen = false;
        await _connection.StopAsync();
    }

    public Task StartedStudyingAsync(Guid subjectId, SessionMode mode)
    {
        _studying = (subjectId, mode);
        return SendAsync("StartedStudying", subjectId, mode);
    }

    public Task StoppedStudyingAsync()
    {
        _studying = null;
        return SendAsync("StoppedStudying");
    }

    public Task KeepPresenceAsync() => SendAsync("KeepPresence");

    public Task JoinMyGroupsAsync() => SendAsync("JoinMyGroups");

    public Task SubscribeLeaderboardAsync()
    {
        _leaderboardOpen = true;
        return SendAsync("SubscribeLeaderboard");
    }

    public Task UnsubscribeLeaderboardAsync()
    {
        _leaderboardOpen = false;
        return SendAsync("UnsubscribeLeaderboard");
    }

    public Task SendMessageAsync(Guid groupId, string body) =>
        _connection.State == HubConnectionState.Connected
            ? _connection.InvokeAsync("SendMessage", groupId, body)
            : throw new HubException("Not connected to the server.");

    public Task LeaveGroupChannelAsync(Guid groupId) => SendAsync("LeaveGroupChannel", groupId);

    private async Task SendAsync(string method, params object?[] args)
    {
        if (_connection.State != HubConnectionState.Connected)
            return;
        try
        {
            await _connection.SendCoreAsync(method, args);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            // Lost the connection mid-call; the reconnect handler restores what matters.
        }
    }
}
