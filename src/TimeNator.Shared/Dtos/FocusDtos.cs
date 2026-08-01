namespace TimeNator.Shared.Dtos;

public record AllowedAppRequest(string ProcessName, string DisplayName);

public record AllowedAppResponse(Guid Id, string ProcessName, string DisplayName);

public record DistractionEventRequest(
    Guid? SessionId,
    string ProcessName,
    string WindowTitle,
    DateTimeOffset OccurredAt,
    int DurationSeconds);

public record DistractionEventResponse(
    Guid Id,
    Guid? SessionId,
    string ProcessName,
    string WindowTitle,
    DateTimeOffset OccurredAt,
    int DurationSeconds);
