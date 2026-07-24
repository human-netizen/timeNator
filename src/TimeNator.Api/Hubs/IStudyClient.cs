using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Hubs;

/// <summary>Calls the server makes on connected clients. Typed so a renamed method fails to compile.</summary>
public interface IStudyClient
{
    Task MemberStarted(Guid groupId, MemberPresence presence);
    Task MemberStopped(Guid groupId, Guid userId);
}
