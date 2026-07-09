namespace TimeNator.Shared.Dtos;

public record CreateSessionRequest(
    Guid SubjectId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int DurationSeconds,
    int PausedSeconds,
    SessionMode Mode,
    SessionSource Source,
    int MaxStreakSeconds);

public record SessionResponse(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    string SubjectColorHex,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int DurationSeconds,
    int PausedSeconds,
    SessionMode Mode,
    SessionSource Source,
    int MaxStreakSeconds);
