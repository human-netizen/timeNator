namespace TimeNator.Api.Entities;

/// <summary>A stretch of a session spent in an application that is not on the allowed list.</summary>
public class DistractionEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public required string ProcessName { get; set; }
    public required string WindowTitle { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int DurationSeconds { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public StudySession? Session { get; set; }
}
