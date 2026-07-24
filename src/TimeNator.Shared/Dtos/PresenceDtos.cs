namespace TimeNator.Shared.Dtos;

/// <summary>
/// Someone who is studying right now. Clients compute the running time locally from
/// <see cref="StartedAt"/>; the server never pushes per-second updates.
/// </summary>
public record MemberPresence(
    Guid UserId,
    string DisplayName,
    Guid SubjectId,
    string SubjectName,
    string SubjectColorHex,
    DateTimeOffset StartedAt,
    SessionMode Mode);
