namespace TimeNator.Shared.Dtos;

/// <summary>
/// <see cref="RepeatRule"/> is "daily", "weekdays" or "weekly:mon,wed,fri". A repeating
/// to-do needs a due date, which is the first day it applies.
/// </summary>
public record CreateTodoRequest(string Title, Guid? SubjectId, DateOnly? DueDate, string? RepeatRule);

public record UpdateTodoRequest(string Title, Guid? SubjectId, DateOnly? DueDate, bool IsDone);

public record TodoResponse(
    Guid Id,
    string Title,
    Guid? SubjectId,
    string? SubjectName,
    DateOnly? DueDate,
    bool IsDone,
    DateTimeOffset? CompletedAt,
    string? RepeatRule,
    Guid? RepeatParentId);
