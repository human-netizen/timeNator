using TimeNator.Shared;

namespace TimeNator.Api.Entities;

public class StudySession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public int PausedSeconds { get; set; }
    public SessionMode Mode { get; set; }
    public SessionSource Source { get; set; }
    public int MaxStreakSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
