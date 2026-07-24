using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using TimeNator.Api.Controllers;
using TimeNator.Api.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Hubs;

/// <summary>
/// The realtime channel. Presence is derived from the connection: starting a timer
/// announces it, stopping or disconnecting withdraws it. Each study group is a SignalR
/// group named group:{id}, joined on connect.
/// </summary>
[Authorize]
public class StudyHub(GroupService groups, SubjectService subjects, PresenceService presence)
    : Hub<IStudyClient>
{
    public static string GroupName(Guid groupId) => $"group:{groupId}";

    private Guid UserId => Context.User!.GetUserId();

    public override async Task OnConnectedAsync()
    {
        await JoinMyGroups();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await StoppedStudying();
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Also called by the client after it joins a group, so it hears that group without reconnecting.</summary>
    public async Task JoinMyGroups()
    {
        foreach (var groupId in await groups.ListMyGroupIdsAsync(UserId, Context.ConnectionAborted))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(groupId));
    }

    public async Task StartedStudying(Guid subjectId, SessionMode mode)
    {
        var subject = await subjects.FindAsync(UserId, subjectId, Context.ConnectionAborted)
                      ?? throw new HubException("Unknown subject.");

        var displayName = Context.User!.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? "";
        var current = new MemberPresence(UserId, displayName, subject.Id, subject.Name, subject.ColorHex,
            DateTimeOffset.UtcNow, mode);
        await presence.SetAsync(current);

        foreach (var groupId in await groups.ListMyGroupIdsAsync(UserId, Context.ConnectionAborted))
            await Clients.Group(GroupName(groupId)).MemberStarted(groupId, current);
    }

    public async Task StoppedStudying()
    {
        await presence.ClearAsync(UserId);
        foreach (var groupId in await groups.ListMyGroupIdsAsync(UserId, CancellationToken.None))
            await Clients.Group(GroupName(groupId)).MemberStopped(groupId, UserId);
    }

    /// <summary>
    /// Sent every couple of minutes while a timer runs. It only extends the presence TTL;
    /// it says nothing about how long the user studied, which the server never verifies.
    /// </summary>
    public Task KeepPresence() => presence.RefreshAsync(UserId);
}
