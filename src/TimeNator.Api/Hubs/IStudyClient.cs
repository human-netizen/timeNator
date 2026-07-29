using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Hubs;

/// <summary>Calls the server makes on connected clients. Typed so a renamed method fails to compile.</summary>
public interface IStudyClient
{
    Task MemberStarted(Guid groupId, MemberPresence presence);
    Task MemberStopped(Guid groupId, Guid userId);
    Task LeaderboardUpdated(List<LeaderboardEntry> entries);
    Task MessageReceived(Guid groupId, GroupMessageItem message);

    /// <summary>Group settings or member permissions changed; clients refetch what they show.</summary>
    Task GroupUpdated(Guid groupId);

    Task MemberRemoved(Guid groupId, Guid userId);
}
